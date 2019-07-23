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

      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );
        Debug.Print( e.Name );
      }
      return Result.Succeeded;
    }
  }
}
