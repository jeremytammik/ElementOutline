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

      Dictionary<int, JtLoops> booleanLoops
        = new Dictionary<int, JtLoops>( 
          ids.Count<ElementId>() );

      foreach( ElementId id in ids )
      {
        Room room = doc.GetElement( id ) as Room;

        JtLoops loops 
          = Cmd2dBoolean.GetRoomOuterBoundaryLoops(
            room, seb_opt, view );

        if( null == loops ) // the room may not be bounded
        {
          continue;
        }
        booleanLoops.Add( id.IntegerValue, loops );
      }

      JtWindowHandle hwnd = new JtWindowHandle(
        uiapp.MainWindowHandle );

      Cmd2dBoolean.CreateOutput( "room_outer_outline",
        "Room Outer Outline", doc, hwnd, booleanLoops );

      return Result.Succeeded;
    }
  }
}
