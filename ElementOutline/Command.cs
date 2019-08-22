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
#endregion

namespace ElementOutline
{
  [Transaction( TransactionMode.ReadOnly )]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Retrieve plan view boundary loops from element 
    /// solids using ExtrusionAnalyzer.
    /// </summary>
    static Dictionary<int, JtLoops> GetSolidLoops(
      Document doc,
      ICollection<ElementId> ids )
    {
      Dictionary<int, JtLoops> solidLoops
        = new Dictionary<int, JtLoops>();

      int nFailures;

      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        if( e is Dimension )
        {
          continue;
        }

        Debug.Print( e.Name + " "
          + id.IntegerValue.ToString() );

        nFailures = 0;

        JtLoops loops
          = CmdUploadRooms.GetSolidPlanViewBoundaryLoops(
            e, false, ref nFailures );

        if( 0 < nFailures )
        {
          Debug.Print( "{0}: {1}",
            Util.ElementDescription( e ),
            Util.PluralString( nFailures,
              "extrusion analyser failure" ) );
        }
        CmdUploadRooms.ListLoops( e, loops );

        solidLoops.Add( id.IntegerValue, loops );
      }
      return solidLoops;
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

      IntPtr hwnd = uiapp.MainWindowHandle;

      if( null == doc )
      {
        Util.ErrorMsg( "Please run this command in a valid"
          + " Revit project document." );
        return Result.Failed;
      }

      // Ensure that output folder exists -- always fails

      //if( !File.Exists( _output_folder_path ) )
      //{
      //  Util.ErrorMsg( string.Format(
      //    "Please ensure that output folder '{0}' exists",
      //    _output_folder_path ) );
      //  return Result.Failed;
      //}

      ICollection<ElementId> ids 
        = Util.GetSelectedElements( uidoc );

      if( (null == ids) || (0 == ids.Count) )
      {
        return Result.Cancelled;
      }

      // First attempt: create element 2D outline from
      // element geometry solids using the ExtrusionAnalyzer;
      // unfortunately, some elements have no valid solid,
      // so this approach is not general enough.

      // Map element id to its solid outline loops

      Dictionary<int, JtLoops> solidLoops = GetSolidLoops(
        doc, ids );

      string filepath = Path.Combine( Util.OutputFolderPath,
        doc.Title + "_element_solid_outline.json" );

      Util.ExportLoops( filepath, doc, solidLoops );

      // Second attempt: create element 2D outline from
      // element geometry edges in current view by 
      // projecting them onto the XY plane and then 
      // following the outer contour
      // counter-clockwise keeping to the right-most
      // edge until a closed loop is achieved.

      bool second_attempt = false;

      if( second_attempt )
      {
        View view = doc.ActiveView;

        Options opt = new Options
        {
          View = view
        };

        EdgeLoopRetriever edgeLooper
          = new EdgeLoopRetriever( opt, ids );

        filepath = Path.Combine( Util.OutputFolderPath,
           doc.Title + "_element_edge_outline.json" );

        Util.ExportLoops( filepath, doc, edgeLooper.Loops );
      }
      return Result.Succeeded;
    }
  }
}
