#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
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
    static double _min_len = Util.ConvertMillimetresToFeet( 1 );
    static double _step_len = Util.ConvertMillimetresToFeet( 5 );

    /// <summary>
    /// Add a new segment to the collection, 
    /// avoiding duplication
    /// </summary>
    void AddSegment2( 
      Point2dInt p, 
      Point2dInt q )
    {
      if(!ContainsKey(p))
      {
        Add( p, new List<Point2dInt>( 1 ) );
      }
      if(!this[p].Contains(q))
      {
        this[ p ].Add( q );
      }
    }

    /// <summary>
    /// Add a new segment to the collection in both
    /// direcxtions, so that both of its endpoints
    /// show up as dictionary keys.
    /// </summary>
    void AddSegment(
      Point2dInt a,
      Point2dInt b )
    {
      AddSegment2( a, b );
      AddSegment2( b, a );
    }

    /// <summary>
    /// Initialise the collection of 2D integer 
    /// millimetre coordinate line segments from
    /// the given Revit curves in feet.
    /// </summary>
    public JtLineCollection( List<Curve> curves )
    {
      foreach( Curve c in curves )
      {
        double len = c.Length;

        if( len < _min_len )
        {
          continue;
        }

        Point2dInt a = new Point2dInt( 
          c.GetEndPoint( 0 ) );

        Point2dInt b;

        if( len < _step_len )
        {
          b = new Point2dInt( c.GetEndPoint( 1 ) );
          AddSegment( a, b );
          AddSegment( b, a );
        }

        int nSegments = (int) Math.Round( len / _step_len,
          MidpointRounding.AwayFromZero );

        double sp = c.GetEndParameter( 0 );
        double ep = c.GetEndParameter( 1 );
        double step = (ep - sp) / nSegments;

        double t = sp + step;

        for( int i = 0; i < nSegments; ++i, t += step )
        {
          b = new Point2dInt( c.Evaluate( t, false ) );

          if( 0 != a.CompareTo(b))
          {
            AddSegment( a, b );
            AddSegment( b, a );
            a = b;
          }
        }
      }
    }
  }
}
