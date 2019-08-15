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
      if( !ContainsKey( p ) )
      {
        Add( p, new List<Point2dInt>( 1 ) );
      }
      if( !this[ p ].Contains( q ) )
      {
        this[ p ].Add( q );
      }
    }

    /// <summary>
    /// Add a new segment to the collection in both
    /// directions, so that both of its endpoints
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
    /// the given Revit 3D curves in feet.
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

          if( 0 != a.CompareTo( b ) )
          {
            AddSegment( a, b );
            a = b;
          }
        }
      }
    }


    class Point2dIntAngleComparer : IComparer<Point2dInt>
    {
      Point2dInt _current;
      double _current_angle;

      public Point2dIntAngleComparer(
        Point2dInt current,
        double current_angle )
      {
        Debug.Assert( -Math.PI < current_angle,
          "expected current_angle in interval (-pi,pi]" );

        Debug.Assert( current_angle <= -Math.PI, 
          "expected current_angle in interval (-pi,pi]" );

        _current = current;
        _current_angle = current_angle;
      }

      /// <summary>
      /// Order the target points by angle.
      /// Right-most comes first, so small comes before large.
      /// However, current_angle defines the origin of comparison.
      /// Therefore, if ax is only slightly larger than current_angle
      /// and ay is slightly smaller, ay is considered ay + 2 * pi.
      /// If the angles are equal, the closer of the two point is 
      /// considered smaller.
      /// </summary>
      public int Compare( Point2dInt x, Point2dInt y )
      {
        double ax = _current.AngleTo( x ); // (-pi,pi]
        double ay = _current.AngleTo( y );

        if( Util.IsEqual(ax,ay))
        {
          double dx = _current.DistanceTo( x );
          double dy = _current.DistanceTo( y );
          return dx.CompareTo( dy );
        }

        if( ax < _current_angle )
        {
          ax += 2 * Math.PI;
        }
        if( ay < _current_angle )
        {
          ay += 2 * Math.PI;
        }
        int d = ax.CompareTo( ay );
        if(0==d)
        {
          double dx = _current.DistanceTo( x );
          double dy = _current.DistanceTo( y );
          d = dx.CompareTo( dy );
        }
        return d;
      }
    }

    public bool GetOutlineRecursion(
      List<Point2dInt> route )
    {
      Point2dInt endpoint = route[ 0 ];

      int n = route.Count;
      Point2dInt current = route[ n - 1 ];
      List<Point2dInt> candidates = this[ current ];
      if(candidates.Contains(endpoint))
      {
        // A closed loop has been completed
        return true;
      }

      // At the left-most point, try going downwards,
      // else try the right-most possibility taking
      // the current direction into account

      double current_angle = (1 == n)
        ? -0.5 * Math.PI
        : route[ n - 2 ].AngleTo( current );

      candidates.Sort( new Point2dIntAngleComparer( 
        current, current_angle ) );

      foreach( Point2dInt cand in candidates )
      {
        route.Add( cand );
        Debug.Assert( n + 1 == route.Count, 
          "expected exactly one candidate added" );

        if(GetOutlineRecursion(route))
        {
          return true;
        }

        Debug.Assert( n + 1 == route.Count, 
          "expected exactly one candidate added" );
        route.RemoveAt( n );
      }

      // We tried all candidates and found 
      // no closed loop, so retreat

      return false;
    }

    /// <summary>
    /// Return the outline loop of all the line segments
    /// </summary>
    public JtLoop GetOutline()
    {
      // Outline route taken so far

      List<Point2dInt> route = new List<Point2dInt>(1);

      // Start at minimum point

      route.Add( Keys.Min() );

      // Recursively search until a closed outline is found

      bool closed = GetOutlineRecursion( route );

      return closed 
        ? new JtLoop( route.ToArray() ) 
        : null;
    }
  }
}
