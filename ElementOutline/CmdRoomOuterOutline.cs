#region Namespaces
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
#endregion

namespace ElementOutline
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdRoomOuterOutline : IExternalCommand
  {
    /// <summary>
    /// Return the elements bounding the first and 
    /// hopefully outermost boundary loop of the
    /// given room. Ignore holes in the room and 
    /// multiple disjunct parts.
    /// </summary>
    static List<ElementId> GetRoomBoundaryIds( 
      Room room,
      SpatialElementBoundaryOptions seb_opt )
    {
      List<ElementId> ids = null;

      IList<IList<BoundarySegment>> sloops
        = room.GetBoundarySegments( seb_opt );

      if( null != sloops ) // the room may not be bounded
      {
        Debug.Assert( 1 == sloops.Count, "this add-in "
          + "currently supports only rooms with one "
          + "single boundary loop" );

        ids = new List<ElementId>();

        foreach( IList<BoundarySegment> sloop in sloops )
        {
          foreach( BoundarySegment s in sloop )
          {
            ids.Add( s.ElementId );
          }

          // Skip out after first segement loop - ignore
          // rooms with holes and disjunct parts

          break;
        }
      }
      return ids;
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

      IEnumerable<ElementId> ids 
        = Util.GetSelectedRooms( uidoc );

      if( (null == ids) || (0 == ids.Count()) )
      {
        return Result.Cancelled;
      }

      View view = doc.ActiveView;

      SpatialElementBoundaryOptions seb_opt
        = new SpatialElementBoundaryOptions();

      foreach( ElementId id in ids )
      {
        Room room = doc.GetElement( id ) as Room;

        JtLoop loop 
          = Cmd2dBoolean.GetRoomOuterBoundaryLoop( 
            room, seb_opt, view );

        if( null == loop ) // the room may not be bounded
        {
          continue;
        }


        List<ElementId> boundary_ids 
          = GetRoomBoundaryIds( room, seb_opt );
        IList<IList<BoundarySegment>> sloops
          = room.GetBoundarySegments( seb_opt );


        foreach( IList<BoundarySegment> sloop in sloops )
        {
          loop = new CurveLoop();

          foreach( BoundarySegment s in sloop )
          {
            loop.Append( s.GetCurve() );

            ElementType type = doc.GetElement(
              s.ElementId ) as ElementType;

            Element e = doc.GetElement( s.ElementId );
          }

          // Todo: tessellate each segment curve, convert
          // to integer mm-based cooreds, create a 2D Boolean,
          // and union all the surrounding wall and other 
          // elements with that

          // Skip out after first segement loop - ignore
          // rooms with holes and disjunct parts

          break;
        }
      }
      return Result.Succeeded;
    }
  }
}
