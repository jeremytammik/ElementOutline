#region Namespaces
using System.Collections.Generic;
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

      SpatialElementBoundaryOptions seb_opt
        = new SpatialElementBoundaryOptions();

      CurveLoop loop = null;

      foreach( ElementId id in ids )
      {
        Room room = doc.GetElement( id ) as Room;

        IList<IList<BoundarySegment>> sloops
          = room.GetBoundarySegments( seb_opt );

        if( null == sloops ) // the room may not be bounded
        {
          continue;
        }

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
