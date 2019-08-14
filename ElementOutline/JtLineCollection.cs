#region Namespaces
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
#endregion

namespace ElementOutline
{
  /// <summary>
  /// Store line segment data twice over, with both 
  /// endpoints entered as keys, pointing to a list 
  /// of all the corresponding other endpoints
  /// </summary>
  class JtLineCollection : Dictionary<Point2dInt, List<Point2dInt>>
  {
  }
}
