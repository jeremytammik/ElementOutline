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
      Clipper c = new Clipper();
      List<List<IntPoint>> union = new List<List<IntPoint>>();

      c.Execute( ClipType.ctUnion, union, 
        PolyFillType.pftPositive, PolyFillType.pftPositive );

      return Result.Succeeded;
    }
  }
}
