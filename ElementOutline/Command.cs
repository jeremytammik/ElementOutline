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

    static void ExportLoops(
      string filepath,
      Document doc,
      Dictionary<int, JtLoops> loops )
    {
      using( StreamWriter s = new StreamWriter( filepath ) )
      {
        List<int> keys = new List<int>( loops.Keys );
        keys.Sort();
        foreach( int key in keys )
        {
          ElementId id = new ElementId( key );
          Element e = doc.GetElement( id );

          s.WriteLine(
            "{{\"name\":\"{0}\", \"id\":\"{1}\", "
            + "\"uid\":\"{2}\", \"svg_path\":\"{3}\"}}",
            e.Name, e.Id, e.UniqueId,
            loops[ key ].SvgPath );
        }
        s.Close();
      }
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

      // Do we have any pre-selected elements?

      Selection sel = uidoc.Selection;

      ICollection<ElementId> ids = sel.GetElementIds();

      // If no elements were pre-selected, 
      // prompt for post-selection

      if( null == ids || 0 == ids.Count )
      {
        IList<Reference> refs = null;

        try
        {
          refs = sel.PickObjects( ObjectType.Element,
            "Please select elements for 2D outline generation." );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return Result.Cancelled;
        }
        ids = new List<ElementId>(
          refs.Select<Reference, ElementId>(
            r => r.ElementId ) );
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

      ExportLoops( filepath, doc, solidLoops );

      // Second attempt: create element 2D outline from
      // element geometry edges in current view by 
      // projecting them onto the XY plane and then 
      // following the outer contour
      // counter-clockwise keeping to the right-most
      // edge until a closed loop is achieved.

      View view = doc.ActiveView;

      Options opt = new Options
      {
        View = view
      };

      EdgeLoopRetriever edgeLooper
        = new EdgeLoopRetriever( opt, ids );

      filepath = Path.Combine( Util.OutputFolderPath,
         doc.Title + "_element_edge_outline.json" );

      ExportLoops( filepath, doc, edgeLooper.Loops );

      return Result.Succeeded;
    }
  }
}
