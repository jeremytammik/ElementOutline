#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.IO;
using ClipperLib;
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

      Options opt = new Options
      {
        View = view
      };

      Clipper c = new Clipper();
      List<List<IntPoint>> union = new List<List<IntPoint>>();

      c.Execute( ClipType.ctUnion, union,
        PolyFillType.pftPositive, PolyFillType.pftPositive );

      Dictionary<int, JtLoops> booleanLoops 
        = new Dictionary<int, JtLoops>( ids.Count );

      string filepath = Path.Combine( Util.OutputFolderPath,
         doc.Title + "_element_2d_boolean_outline.json" );

      Util.ExportLoops( filepath, doc, booleanLoops );

      return Result.Succeeded;
    }
  }
}
