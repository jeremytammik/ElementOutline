#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace ElementOutline
{
  /// <summary>
  /// Retrieve plan view boundary loops from element 
  /// geometry edges; create element 2D outline from
  /// element geometry edges by projecting them onto 
  /// the XY plane and then following the outer contour
  /// counter-clockwise keeping to the right-most
  /// edge until a closed loop is achieved.
  /// </summary>
  class EdgeLoopRetriever
  {
    Dictionary<int, JtLoops> _loops
      = new Dictionary<int, JtLoops>();

    public EdgeLoopRetriever( 
      Document doc,
      ICollection<ElementId> ids )
    {
    }

    public Dictionary<int, JtLoops> Loops
    {
      get { return _loops; }
    }
  }
}
