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

    /// <summary>
    /// Return loops for outer 2D outline 
    /// of given element.
    /// - Retrieve geometry curves from edges 
    /// - Convert to linear segments and 2D integer coordinates
    /// - Convert to non-intersecting line segments
    /// - Start from left-hand bottom point
    /// - Go down, then right
    /// - Keep following right-mostconection until closed loop is found
    /// </summary>
    JtLoops GetLoops( Element e )
    {
      JtLoops loops = null;
      return loops;
    }

    public EdgeLoopRetriever( 
      Document doc,
      ICollection<ElementId> ids )
    {
      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        JtLoops loops = GetLoops( e );

        _loops.Add( id.IntegerValue, loops );
      }
    }

    public Dictionary<int, JtLoops> Loops
    {
      get { return _loops; }
    }
  }
}
