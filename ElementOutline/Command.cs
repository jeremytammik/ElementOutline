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
#endregion

namespace ElementOutline
{
  [Transaction( TransactionMode.ReadOnly )]
  public class Command : IExternalCommand
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

      IntPtr hwnd = uiapp.MainWindowHandle;

      if( null == doc )
      {
        Util.ErrorMsg( "Please run this command in a valid"
          + " Revit project document." );
        return Result.Failed;
      }

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

      // Map element id to its outline loops

      Dictionary<int, JtLoops> elementLoops
        = new Dictionary<int, JtLoops>();

      int nFailures;

      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        Debug.Print( e.Name + " " 
          + id.IntegerValue.ToString() );

        nFailures = 0;

        JtLoops loops 
          = CmdUploadRooms.GetPlanViewBoundaryLoops(
            e, false, ref nFailures );

        if( 0 < nFailures )
        {
          Debug.Print( "{0}: {1}",
            Util.ElementDescription( e ),
            Util.PluralString( nFailures,
              "extrusion analyser failure" ) );
        }
        CmdUploadRooms.ListLoops( e, loops );

        elementLoops.Add( id.IntegerValue, loops );
      }
      return Result.Succeeded;
    }
  }
}
