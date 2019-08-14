#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace ElementOutline
{
  /// <summary>
  /// Retrieve plan view boundary loops from element 
  /// geometry edges; create element 2D outline from
  /// element geometry edges by projecting them onto 
  /// the XY plane and then following the outer contour
  /// counter-clockwise keeping to the right-most
  /// edge until a closed loop is achieved.
  /// </summary>
  class EdgeLoopRetriever
  {
    Dictionary<int, JtLoops> _loops
      = new Dictionary<int, JtLoops>();

    /// <summary>
    /// Recursively rtrieve all curves and solids from
    /// thew given geometry
    /// </summary>
    static void AddCurvesAndSolids( 
      GeometryElement geoElem,
      List<Curve> curves,
      List<Solid> solids )
    {
      foreach( GeometryObject obj in geoElem )
      {
        Curve curve = obj as Curve;
        if( null != curve )
        {
          curves.Add( curve );
          continue;
        }
        Solid solid = obj as Solid;
        if( null != solid )
        {
          solids.Add( solid );
          continue;
        }
        GeometryInstance inst = obj as GeometryInstance;
        if( null != inst )
        {
          GeometryElement txGeoElem
            = inst.GetInstanceGeometry( 
              inst.Transform );

          AddCurvesAndSolids( txGeoElem, 
            curves, solids );
        }
      }
    }

    List<Curve> GetCurves( Element e, Options opt )
    {
      GeometryElement geo = e.get_Geometry( opt );

      List<Curve> curves = new List<Curve>();
      List<Solid> solids = new List<Solid>();

      AddCurvesAndSolids( geo, curves, solids );

      return curves;
    }

    /// <summary>
    /// Return loops for outer 2D outline 
    /// of given element.
    /// - Retrieve geometry curves from edges 
    /// - Convert to linear segments and 2D integer coordinates
    /// - Convert to non-intersecting line segments
    /// - Start from left-hand bottom point
    /// - Go down, then right
    /// - Keep following right-mostconection until closed loop is found
    /// </summary>
    JtLoops GetLoops( Element e, Options opt )
    {

      List<Curve> curves = GetCurves( e, opt );
      JtLoops loops = null;
      return loops;
    }

    public EdgeLoopRetriever(
      Options opt,
      ICollection<ElementId> ids )
    {
      Document doc = opt.View.Document;

      List<Curve> curves = new List<Curve>();
      List<Solid> solids = new List<Solid>();

      foreach( ElementId id in ids )
      {
        curves.Clear();
        solids.Clear();

        // Retrieve element geometry

        Element e = doc.GetElement( id );
        GeometryElement geo = e.get_Geometry( opt );
        AddCurvesAndSolids( geo, curves, solids );

        // Extract curves from solids

        //AddCurvesFromSolids( curves, solids );

        // Flatten and simplify to line unique segments 
        // of non-zero length with 2D integer millimetre 
        // coordinates

        // Chop at each intersection, eliminating all
        // non-endpoint intersections

        // Contour following

        JtLoops loops = GetLoops( e, opt );

        _loops.Add( id.IntegerValue, loops );
      }
    }

    public Dictionary<int, JtLoops> Loops
    {
      get { return _loops; }
    }
  }
}
