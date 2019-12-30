#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using ClipperLib;
#endregion

namespace ElementOutline
{
  using Polygon = List<IntPoint>;
  using Polygons = List<List<IntPoint>>;
  using LineSegment = Tuple<IntPoint, IntPoint>;

  [Transaction( TransactionMode.ReadOnly )]
  class Cmd2dBoolean : IExternalCommand
  {
    /// <summary>
    /// Map Point2dInt coordinates to 
    /// Clipper IntPoint instances. 
    /// The purpose of this is that depending on the 
    /// precision used by the comparison operator,
    /// different Point2dInt input keys may actually
    /// map to the same IntPoint value.
    /// </summary>
    public class VertexLookup : Dictionary<Point2dInt, IntPoint>
    {
      public IntPoint GetOrAdd( XYZ p )
      {
        Point2dInt q = new Point2dInt( p );
        if( !ContainsKey( q ) )
        {
          Add( q, new IntPoint { X = q.X, Y = q.Y } );
        }
        return this[ q ];
      }
    }

    /// <summary>
    /// Add the 2D projection of the given mesh triangles
    /// to the current element outline union
    /// </summary>
    static public bool AddToUnion(
      Polygons union,
      VertexLookup vl,
      Clipper c,
      Mesh m )
    {
      int n = m.NumTriangles;

      Polygons triangles = new Polygons( n );
      Polygon triangle = new Polygon( 3 );

      for( int i = 0; i < n; ++i )
      {
        MeshTriangle mt = m.get_Triangle( i );

        triangle.Clear();
        triangle.Add( vl.GetOrAdd( mt.get_Vertex( 0 ) ) );
        triangle.Add( vl.GetOrAdd( mt.get_Vertex( 1 ) ) );
        triangle.Add( vl.GetOrAdd( mt.get_Vertex( 2 ) ) );
        triangles.Add( triangle );
      }
      return c.AddPaths( triangles, PolyType.ptSubject, true );
    }

    /// <summary>
    /// Add the 2D projection of the given face 
    /// to the current element outline union
    /// </summary>
    static public bool AddToUnion(
      Polygons union,
      VertexLookup vl,
      Clipper c,
      Face f )
    {
      IList<CurveLoop> loops = f.GetEdgesAsCurveLoops();

      // ExporterIFCUtils class can also be used for 
      // non-IFC purposes. The SortCurveLoops method 
      // sorts curve loops (edge loops) so that the 
      // outer loops come first.

      IList<IList<CurveLoop>> sortedLoops
        = ExporterIFCUtils.SortCurveLoops( loops );

      int n = loops.Count;

      Debug.Assert( 0 < n,
        "expected at least one face loop" );

      Polygons faces = new Polygons( n );
      Polygon face2d = new Polygon( loops[ 0 ].NumberOfCurves() );

      //foreach( IList<CurveLoop> loops2
      //  in sortedLoops )

      foreach( CurveLoop loop in loops )
      {
        // Outer curve loops are counter-clockwise

        if( loop.IsCounterclockwise( XYZ.BasisZ ) )
        {
          face2d.Clear();

          foreach( Curve curve in loop )
          {
            IList<XYZ> pts = curve.Tessellate();

            IntPoint a = vl.GetOrAdd( pts[ 0 ] );

            face2d.Add( a );

            n = pts.Count;

            for( int i = 1; i < n; ++i )
            {
              IntPoint b = vl.GetOrAdd( pts[ i ] );

              if( b != a )
              {
                face2d.Add( b );
                a = b;
              }
            }
          }
          faces.Add( face2d );
        }
      }
      return c.AddPaths( faces, PolyType.ptSubject, true );
    }

    /// <summary>
    /// Add the 2D projection of the given arc 
    /// to the current element outline union
    /// </summary>
    static public bool AddToUnion(
      Polygons union,
      VertexLookup vl,
      Clipper c,
      Arc arc )
    {
      IList<XYZ> pts = arc.Tessellate();
      int n = pts.Count;

      Polygons faces = new Polygons( 1 );
      Polygon face2d = new Polygon( n );

      IntPoint a = vl.GetOrAdd( pts[ 0 ] );

      face2d.Add( a );

      for( int i = 1; i < n; ++i )
      {
        IntPoint b = vl.GetOrAdd( pts[ i ] );

        if( b != a )
        {
          face2d.Add( b );
          a = b;
        }
      }
      faces.Add( face2d );

      return c.AddPaths( faces, PolyType.ptSubject, true );
    }

    /// <summary>
    /// Return the union of all outlines projected onto
    /// the XY plane from the geometry solids and meshes
    /// </summary>
    static public bool AddToUnion(
      Polygons union,
      List<LineSegment> curves,
      VertexLookup vl,
      Clipper c,
      GeometryElement geoElem )
    {
      foreach( GeometryObject obj in geoElem )
      {
        // Curve
        // Edge
        // Face
        // GeometryElement
        // GeometryInstance
        // Mesh
        // Point
        // PolyLine
        // Profile
        // Solid

        // Skip objects that contribute no 2D surface

        Curve curve = obj as Curve;
        if( null != curve )
        {
          Arc arc = curve as Arc;

          if( null != arc && arc.IsCyclic )
          {
            AddToUnion( union, vl, c, arc );
          }
          else if( curve.IsBound )
          {
            curves.Add( new LineSegment(
              vl.GetOrAdd( curve.GetEndPoint( 0 ) ),
              vl.GetOrAdd( curve.GetEndPoint( 1 ) ) ) );
          }
          continue;
        }

        Solid solid = obj as Solid;
        if( null != solid )
        {
          foreach( Face f in solid.Faces )
          {
            // Skip pretty common case: vertical planar face

            if( f is PlanarFace
              && Util.IsHorizontal( ((PlanarFace) f).FaceNormal ) )
            {
              continue;
            }
            AddToUnion( union, vl, c, f );
          }
          continue;
        }

        Mesh mesh = obj as Mesh;
        if( null != mesh )
        {
          AddToUnion( union, vl, c, mesh );
          continue;
        }

        GeometryInstance inst = obj as GeometryInstance;
        if( null != inst )
        {
          GeometryElement txGeoElem
            = inst.GetInstanceGeometry(
              Transform.Identity ); // inst.Transform

          AddToUnion( union, curves, vl, c, txGeoElem );
          continue;
        }
        Debug.Assert( false,
          "expected only solid, mesh or instance" );
      }
      return true;
    }

    /// <summary>
    /// Return the union of the outermost room boundary
    /// loop projected onto the XY plane.
    /// </summary>
    static public bool AddToUnionRoom(
      Polygons union,
      List<LineSegment> curves,
      VertexLookup vl,
      Clipper c,
      IList<IList<BoundarySegment>> boundary )
    {
      int n = boundary.Count;

      Debug.Assert( 0 < n,
        "expected at least one room boundary loop" );

      Polygons faces = new Polygons( n );
      Polygon face2d = new Polygon( boundary[ 0 ].Count );

      foreach( IList<BoundarySegment> loop in boundary )
      {
        // Outer curve loops are counter-clockwise

        face2d.Clear();

        foreach( BoundarySegment s in loop )
        {
          IList<XYZ> pts = s.GetCurve().Tessellate();

          IntPoint a = vl.GetOrAdd( pts[ 0 ] );

          face2d.Add( a );

          n = pts.Count;

          for( int i = 1; i < n; ++i )
          {
            IntPoint b = vl.GetOrAdd( pts[ i ] );

            if( b != a )
            {
              face2d.Add( b );
              a = b;
            }
          }
          faces.Add( face2d );
        }
      }
      return c.AddPaths( faces, PolyType.ptSubject, true );
    }

    /// <summary>
    /// Create a JtLoop representing the 2D outline of 
    /// the given room including all its bounding elements
    /// by creating the inner room boundary loop and 
    /// uniting it with the bounding elements solid faces 
    /// and meshes in the given view, projecting 
    /// them onto the XY plane and executing 2D Boolean 
    /// unions on them.
    /// </summary>
    public static JtLoop GetRoomOuterBoundaryLoop(
      Room room,
      SpatialElementBoundaryOptions seb_opt,
      View view )
    {
      List<ElementId> boundary_ids
        = GetRoomBoundaryIds( room, seb_opt );
      IList<IList<BoundarySegment>> sloops
        = room.GetBoundarySegments( seb_opt );


      Document doc = view.Document;

      Options opt = new Options
      {
        View = view
      };

      Clipper c = new Clipper();
      VertexLookup vl = new VertexLookup();
      List<LineSegment> curves = new List<LineSegment>();
      Polygons union = new Polygons();
      Dictionary<int, JtLoops> booleanLoops
        = new Dictionary<int, JtLoops>( ids.Count );

      foreach( ElementId id in ids )
      {
        c.Clear();
        vl.Clear();
        union.Clear();

        Element e = doc.GetElement( id );

        if( e is Room )
        {
          IList<IList<BoundarySegment>> boundary
            = (e as Room).GetBoundarySegments(
              new SpatialElementBoundaryOptions() );

          // Ignore all loops except first, which is 
          // hopefully outer -- and hopefully the room
          // does not have several disjunct parts.

          AddToUnionRoom( union, curves, vl, c, boundary );
        }
        else
        {
          GeometryElement geo = e.get_Geometry( opt );
          AddToUnion( union, curves, vl, c, geo );
        }

        //AddToUnion( union, vl, c, curves );

        //c.AddPaths( subjects, PolyType.ptSubject, true );
        //c.AddPaths( clips, PolyType.ptClip, true );

        bool succeeded = c.Execute( ClipType.ctUnion, union,
          PolyFillType.pftPositive, PolyFillType.pftPositive );

        if( 0 == union.Count )
        {
          Debug.Print( string.Format(
            "No outline found for {0} <{1}>",
            e.Name, e.Id.IntegerValue ) );
        }
        else
        {
          JtLoops loops = ConvertToLoops( union );

          loops.NormalizeLoops();

          booleanLoops.Add( id.IntegerValue, loops );
        }
      }
      return booleanLoops;
    }


    /// <summary>
    /// Return the outer polygons defined 
    /// by the given line segments
    /// </summary>
    /// <param name="curves"></param>
    /// <returns></returns>
    Polygons CreatePolygons( List<LineSegment> curves )
    {
      Polygons polys = new Polygons();
      IntPoint p1 = curves.Select<LineSegment, IntPoint>( s => s.Item1 ).Min<IntPoint>();
      IntPoint p2 = curves.Select<LineSegment, IntPoint>( s => s.Item2 ).Min<IntPoint>();
      //IntPoint p = Min( p1, p2 );
      return polys;
    }

    /// <summary>
    /// Convert the curves to a polygon 
    /// and add it to the union
    /// </summary>
    public bool AddToUnion(
      Polygons union,
      VertexLookup vl,
      Clipper c,
      List<LineSegment> curves )
    {
      Polygons polys = CreatePolygons( curves );
      return c.AddPaths( polys, PolyType.ptSubject, true );
    }

    /// <summary>
    /// Convert Clipper polygons to JtLoops
    /// </summary>
    static JtLoops ConvertToLoops( Polygons union )
    {
      JtLoops loops = new JtLoops( union.Count );
      JtLoop loop = new JtLoop( union.First<Polygon>().Count );
      foreach( Polygon poly in union )
      {
        loop.Clear();
        foreach( IntPoint p in poly )
        {
          loop.Add( new Point2dInt(
            (int) p.X, (int) p.Y ) );
        }
        loops.Add( loop );
      }
      return loops;
    }

    /// <summary>
    /// Create JtLoops representing the given element
    /// 2D outlines by retrieving the element solid 
    /// faces and meshes in the given view, projecting 
    /// them onto the XY plane and executing 2d Boolean 
    /// unions on them.
    /// </summary>
    public static Dictionary<int, JtLoops>
      GetElementLoops(
        View view,
        ICollection<ElementId> ids )
    {
      Document doc = view.Document;

      Options opt = new Options
      {
        View = view
      };

      Clipper c = new Clipper();
      VertexLookup vl = new VertexLookup();
      List<LineSegment> curves = new List<LineSegment>();
      Polygons union = new Polygons();
      Dictionary<int, JtLoops> booleanLoops
        = new Dictionary<int, JtLoops>( ids.Count );

      foreach( ElementId id in ids )
      {
        c.Clear();
        vl.Clear();
        union.Clear();

        Element e = doc.GetElement( id );

        if( e is Room )
        {
          IList<IList<BoundarySegment>> boundary
            = (e as Room).GetBoundarySegments(
              new SpatialElementBoundaryOptions() );

          // Ignore all loops except first, which is 
          // hopefully outer -- and hopefully the room
          // does not have several disjunct parts.

          AddToUnionRoom( union, curves, vl, c, boundary );
        }
        else
        {
          GeometryElement geo = e.get_Geometry( opt );
          AddToUnion( union, curves, vl, c, geo );
        }

        //AddToUnion( union, vl, c, curves );

        //c.AddPaths( subjects, PolyType.ptSubject, true );
        //c.AddPaths( clips, PolyType.ptClip, true );

        bool succeeded = c.Execute( ClipType.ctUnion, union,
          PolyFillType.pftPositive, PolyFillType.pftPositive );

        if( 0 == union.Count )
        {
          Debug.Print( string.Format(
            "No outline found for {0} <{1}>",
            e.Name, e.Id.IntegerValue ) );
        }
        else
        {
          JtLoops loops = ConvertToLoops( union );

          loops.NormalizeLoops();

          booleanLoops.Add( id.IntegerValue, loops );
        }
      }
      return booleanLoops;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      if( null == doc )
      {
        Util.ErrorMsg( "Please run this command in a valid"
          + " Revit project document." );
        return Result.Failed;
      }

      ICollection<ElementId> ids
        = Util.GetSelectedElements( uidoc );

      if( (null == ids) || (0 == ids.Count) )
      {
        return Result.Cancelled;
      }

      // Third attempt: create the element 2D outline 
      // from element solid faces and meshes in current 
      // view by projecting them onto the XY plane and 
      // executing 2d Boolean unions on them.

      View view = doc.ActiveView;

      Dictionary<int, JtLoops> booleanLoops
        = GetElementLoops( view, ids );

      string filepath = Path.Combine( Util.OutputFolderPath,
         doc.Title + "_element_2d_boolean_outline.json" );

      JtWindowHandle hwnd = new JtWindowHandle(
        uiapp.MainWindowHandle );

      string caption = doc.Title + " 2D Booleans";

      Util.ExportLoops( filepath, hwnd, caption,
        doc, booleanLoops );

      return Result.Succeeded;
    }
  }
}
