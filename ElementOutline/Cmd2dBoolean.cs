#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace ElementOutline
{
  [Transaction( TransactionMode.ReadOnly )]
  class Cmd2dBoolean : IExternalCommand
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
        = ClipperRvt.GetElementLoops( view, ids );

      JtWindowHandle hwnd = new JtWindowHandle(
        uiapp.MainWindowHandle );

      Util.CreateOutput( "element_2d_boolean_outline",
        "2D Booleans", doc, hwnd, booleanLoops );

      return Result.Succeeded;
    }
  }
}
