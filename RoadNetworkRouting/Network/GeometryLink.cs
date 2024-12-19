using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Road;
using RoadNetworkRouting.Geometry;
using EnergyModule;

namespace RoadNetworkRouting.Network;

public abstract class GeometryLink : ILinkPartGenerator
{
    private IQueryPointInfo _pointInfoQuerier;
    private Point3D[] _geometry;
    private GeometrySmoothing _smoothing;

    private IQueryPointInfo PointCache => _pointInfoQuerier ??= CreatePointInfoQuerier(Geometry, _smoothing);

    public static IQueryPointInfo CreatePointInfoQuerier(Point3D[] geometry, GeometrySmoothing smoothing)
    {
        if (smoothing == GeometrySmoothing.CatmullRomUniform)
            return new CatmullRomCurve(geometry, CatmullRomType.Uniform);
        if (smoothing == GeometrySmoothing.CatmullRomCentripetal)
            return new CatmullRomCurve(geometry, CatmullRomType.Centripetal);
        if (smoothing == GeometrySmoothing.CatmullRomChordal)
            return new CatmullRomCurve(geometry, CatmullRomType.Chordal);
        return new CachedLineTools(geometry);
    }

    public GeometrySmoothing Smoothing
    {
        get => _smoothing;
        set
        {
            _smoothing = value;
            _pointInfoQuerier = null;
        }
    }

    /// <summary>
    /// The 3D length of the road link, calculated (on the first call) from its Geometry.
    /// </summary>
    public double LengthM => PointCache.LengthM;

    public Point3D[] Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            _pointInfoQuerier = null;
        }
    }

    public abstract LinkPart[] GenerateLinkParts(double segmentLength = 20);

    public PointInfo GetGeometricData(double metersFromA)
    {
        return PointCache.QueryPointInfo(metersFromA);
    }

    public abstract GeometryLink Clone(Point3D[] newGeometry = null);

    public virtual GeometryLink ConvertCoordinates(CoordinateConverter converter)
    {
        var link = Clone(Geometry.Select(converter.Forward).ToArray());
        return link;
    }
}

public class CatmullRomCurve : IQueryPointInfo
{
    private readonly IList<Point3D> _points;
    private readonly double _alpha;
    private readonly CachedLineTools _cache;
    private readonly CachedLineTools _smoothed;

    public double LengthM => _cache.LengthM;

    /// <summary>
    /// Initializes a Catmull-Rom curve with a list of control points and alpha for smoothness.
    /// </summary>
    /// <param name="points">The list of control points.</param>
    /// <param name="alpha">The alpha value controlling smoothness (0.0 for uniform, 0.5 for centripetal).</param>
    public CatmullRomCurve(IList<Point3D> points, CatmullRomType catmullRomType)
    {
        _points = points;
        _cache = new CachedLineTools(points.ToArray());
        _smoothed = new CachedLineTools(NewCatmullRomCurve.Interpolate(points, 10, CatmullRomType.Centripetal).ToArray());
        Debug.WriteLine(_cache.LengthM + ", " + _smoothed.LengthM);
        Debug.WriteLine(_cache.Cache.Length + ", " + _smoothed.Cache.Length);
        
        if(catmullRomType == CatmullRomType.Centripetal)
            _alpha = 0.5;
        else if (catmullRomType == CatmullRomType.Chordal)
            _alpha = 1;
    }

    /// <summary>
    /// Gets a point and its tangent on the Catmull-Rom spline at a specified distance along the curve.
    /// </summary>
    /// <param name="atDistance">The distance along the curve in meters.</param>
    /// <returns>The interpolated point on the spline.</returns>
    public PointInfo QueryPointInfo(double atDistance)
    {
        return _smoothed.QueryPointInfo(atDistance);
        if (atDistance < 0 || atDistance > _cache.LengthM)
            throw new ArgumentOutOfRangeException(nameof(atDistance), "Distance must be within the curve's length.");

        var res = GetSegmentAndTByDistance(atDistance);

        if (!ValidateSegmentIndex(res.ix))
        {
            return _cache.QueryPointInfo(atDistance);
        }

        var (point, tangent) = GetPoint(res.ix, res.t);
        var (dx, dy) = (tangent.X, tangent.Y);

        // Horizontal distance is the length of the tangent vector projected onto the X-Y plane
        var horizontalDistance = Math.Sqrt(dx * dx + dy * dy);

        // Angle in the horizontal plane (X-Y plane) in radians
        var hAngle = Math.Atan2(dy, dx);
        //Adjust the horizontal angle if necessary (second and third quadrant)
        if (dx < 0 && dy >= 0) hAngle += Math.PI;
        if (dx < 0 && dy < 0) hAngle -= Math.PI;

        // Vertical angle (angle with the horizontal plane) in radians
        var verticalAngle = Math.Atan2(tangent.Z, horizontalDistance);

        return new PointInfo
        {
            Distance = atDistance,
            HorizontalDistance = atDistance, // This is actually 3D
            X = point.X,
            Y = point.Y,
            Z = point.Z,
            Angle = hAngle,
            VerticalAngle = verticalAngle
        };
    }

    /// <summary>
    /// Gets a point on the Catmull-Rom spline at a parameter t for a given segment.
    /// </summary>
    /// <param name="segmentIndex">The index of the segment.</param>
    /// <param name="t">The parameter along the segment (0 to 1).</param>
    /// <returns>The interpolated point on the spline.</returns>
    private (Point3D Location, Point3D Tangent) GetPoint(int segmentIndex, double t)
    {
        var p0 = _points[segmentIndex - 1];
        var p1 = _points[segmentIndex - 0];
        var p2 = _points[segmentIndex + 1];
        var p3 = _points[segmentIndex + 2];

        return EvaluateCatmullRom(p0, p1, p2, p3, t);
    }

    private (int ix, double t) GetSegmentAndTByDistance(double distance)
    {
        // Find the segment that contains the given distance
        for (var i = 0; i < _points.Count; i++)
        {
            if (distance <= _cache.Cache[i].Distance)
            {
                var segmentStartDistance = i > 0 ? _cache.Cache[i - 1].Distance : 0;
                
                var distanceBetween = distance - segmentStartDistance;
                var segmentT = distanceBetween <= 0 ? 0 : distanceBetween / (_cache.Cache[i].Distance - segmentStartDistance);

                return (i - 1, segmentT);
            }
        }

        return (-1, -1);
    }

    private (Point3D Location, Point3D Tangent) EvaluateCatmullRom(Point3D p0, Point3D p1, Point3D p2, Point3D p3, double t)
    {
        double t0 = 0f;
        var t1 = t0 + GetKnotInterval(p0, p1);
        var t2 = t1 + GetKnotInterval(p1, p2);
        var t3 = t2 + GetKnotInterval(p2, p3);

        var u = LerpUnclamped(t1, t2, t);

        var a1 = Remap(t0, t1, p0, p1, u);
        var a2 = Remap(t1, t2, p1, p2, u);
        var a3 = Remap(t2, t3, p2, p3, u);
        var b1 = Remap(t0, t2, a1, a2, u);
        var b2 = Remap(t1, t3, a2, a3, u);

        return (Remap(t1, t2, b1, b2, u), new Point3D(
            (b2.X - b1.X) / (t2 - t1),
            (b2.Y - b1.Y) / (t2 - t1)
        ));
    }

    /// <summary>
    /// Calculates the interval (or "knot distance") between two points on the spline.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The computed interval as a double.</returns>
    /// <remarks>
    /// The interval is raised to the power of 0.5 * alpha, where alpha controls the smoothness:
    /// - Alpha = 0.0: Uniform spline (equal spacing between knots).
    /// - Alpha = 0.5: Centripetal spline (better handling of sharp turns).
    /// - Alpha = 1.0: Chordal spline (maximum distance sensitivity).
    /// </remarks>
    private double GetKnotInterval(Point3D a, Point3D b)
    {
        return 1;// (double)Math.Pow(SqrMagnitude(a, b), 0.5f * _alpha);
    }

    /// <summary>
    /// Computes the squared magnitude (distance squared) between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The squared distance between the two points.</returns>
    /// <remarks>
    /// Squared magnitude is used instead of directly calculating the distance
    /// to avoid unnecessary square root calculations for efficiency.
    /// </remarks>
    private static double SqrMagnitude(Point3D a, Point3D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Maps a value `u` from the range `[a, b]` to interpolate between two points `c` and `d`.
    /// </summary>
    /// <param name="a">The lower bound of the input range.</param>
    /// <param name="b">The upper bound of the input range.</param>
    /// <param name="c">The first point in the output range.</param>
    /// <param name="d">The second point in the output range.</param>
    /// <param name="u">The value to remap, typically within `[a, b]`.</param>
    /// <returns>The interpolated point on the line between `c` and `d`.</returns>
    /// <remarks>
    /// This function is useful for interpolating between multiple levels of
    /// Catmull-Rom calculations, e.g., blending control points to compute the final point.
    /// </remarks>
    private static Point3D Remap(double a, double b, Point3D c, Point3D d, double u)
    {
        var t = (u - a) / (b - a); // Normalize u to [0, 1]
        return Lerp(c, d, t);
    }

    /// <summary>
    /// Linearly interpolates between two points.
    /// </summary>
    /// <param name="a">The starting point.</param>
    /// <param name="b">The ending point.</param>
    /// <param name="t">The interpolation factor, typically between 0.0 and 1.0.</param>
    /// <returns>The interpolated point between `a` and `b`.</returns>
    /// <remarks>
    /// This function is used to blend two points based on the parameter `t`, 
    /// where `t = 0.0` yields `a` and `t = 1.0` yields `b`.
    /// </remarks>
    private static Point3D Lerp(Point3D a, Point3D b, double t)
    {
        return new Point3D(
            a.X + (b.X - a.X) * t, // Interpolate X coordinate
            a.Y + (b.Y - a.Y) * t  // Interpolate Y coordinate
        );
    }

    /// <summary>
    /// Linearly interpolates between two scalar values without clamping `t`.
    /// </summary>
    /// <param name="a">The starting value.</param>
    /// <param name="b">The ending value.</param>
    /// <param name="t">The interpolation factor, which can be outside `[0, 1]`.</param>
    /// <returns>The interpolated value between `a` and `b`.</returns>
    /// <remarks>
    /// Unlike clamped linear interpolation, this allows `t` to extend beyond the range of 0 to 1,
    /// enabling extrapolation.
    /// </remarks>
    private static double LerpUnclamped(double a, double b, double t)
    {
        return a + (b - a) * t;
    }


    private bool ValidateSegmentIndex(int segmentIndex)
    {
        return segmentIndex >= 1 && segmentIndex < _points.Count - 2;
    }
}

public enum CatmullRomType
{
    Chordal,
    Uniform,
    Centripetal
}

public class NewCatmullRomCurve
{
    /**
 * This method will calculate the Catmull-Rom interpolation curve, returning
 * it as a list of Coord coordinate objects.  This method in particular
 * adds the first and last control points which are not visible, but required
 * for calculating the spline.
 *
 * @param coordinates The list of original straight line points to calculate
 * an interpolation from.
 * @param pointsPerSegment The integer number of equally spaced points to
 * return along each curve.  The actual distance between each
 * point will depend on the spacing between the control points.
 * @return The list of interpolated coordinates.
 * @param curveType Chordal (stiff), Uniform(floppy), or Centripetal(medium)
 * @throws gov.ca.water.shapelite.analysis.CatmullRomException if
 * pointsPerSegment is less than 2.
 */
    public static List<Point3D> Interpolate(IList<Point3D> coordinates, int pointsPerSegment, CatmullRomType curveType)
    {
        var vertices = new List<Point3D>();
        foreach (var c in coordinates)
        {
            vertices.Add(c.Clone());
        }

        if (pointsPerSegment < 2)
        {
            throw new Exception("The pointsPerSegment parameter must be greater than 2, since 2 points is just the linear segment.");
        }

        // Cannot interpolate curves given only two points.  Two points
        // is best represented as a simple line segment.
        if (vertices.Count < 3)
        {
            return vertices;
        }

// Test whether the shape is open or closed by checking to see if
// the first point intersects with the last point.  M and Z are ignored.
        var isClosed = false;//vertices[0].intersects2D(vertices[^1]);
        if (isClosed)
        {
            // Use the second and second from last points as control points.
            // get the second point.
            var p2 = vertices[1].Clone();
            // get the point before the last point
            var pn1 = vertices[^2].Clone();

            // insert the second from the last point as the first point in the list
            // because when the shape is closed it keeps wrapping around to
            // the second point.
            vertices.Insert(0, pn1);
            // add the second point to the end.
            vertices.Add(p2);
        }
        else
        {
            // The shape is open, so use control points that simply extend
            // the first and last segments

            // Get the change in x and y between the first and second coordinates.
            var dx = vertices[1].X - vertices[0].X;
            var dy = vertices[1].Y - vertices[0].Y;

            // Then using the change, extrapolate backwards to find a control point.
            var x1 = vertices[0].X - dx;
            var y1 = vertices[0].Y - dy;

            // Actaully create the start point from the extrapolated values.
            var start = new Point3D(x1, y1, vertices[0].Z);

            // Repeat for the end control point.
            var n = vertices.Count - 1;
            dx = vertices[n].X - vertices[n - 1].X;
            dy = vertices[n].Y - vertices[n - 1].Y;
            var xn = vertices[n].X + dx;
            var yn = vertices[n].Y + dy;
            var end = new Point3D(xn, yn, vertices[n].Z);

            // insert the start control point at the start of the vertices list.
            vertices.Insert(0, start);

            // append the end control ponit to the end of the vertices list.
            vertices.Add(end);
        }

// Dimension a result list of coordinates. 
        var result = new List<Point3D>();
// When looping, remember that each cycle requires 4 points, starting
// with i and ending with i+3.  So we don't loop through all the points.
        for (var i = 0; i < vertices.Count - 3; i++)
        {

            // Actually calculate the Catmull-Rom curve for one segment.
            var points = Interpolate(vertices, i, pointsPerSegment, curveType);
            // Since the middle points are added twice, once for each bordering
            // segment, we only add the 0 index result point for the first
            // segment.  Otherwise we will have duplicate points.
            if (result.Count > 0)
            {
                points.RemoveAt(0);
            }

            // Add the coordinates for the segment to the result list.
            result.AddRange(points);
        }

        return result;

    }

    /**
 * Given a list of control points, this will create a list of pointsPerSegment
 * points spaced uniformly along the resulting Catmull-Rom curve.
 *
 * @param points The list of control points, leading and ending with a 
 * coordinate that is only used for controling the spline and is not visualized.
 * @param index The index of control point p0, where p0, p1, p2, and p3 are
 * used in order to create a curve between p1 and p2.
 * @param pointsPerSegment The total number of uniformly spaced interpolated
 * points to calculate for each segment. The larger this number, the
 * smoother the resulting curve.
 * @param curveType Clarifies whether the curve should use uniform, chordal
 * or centripetal curve types. Uniform can produce loops, chordal can
 * produce large distortions from the original lines, and centripetal is an
 * optimal balance without spaces.
 * @return the list of coordinates that define the CatmullRom curve
 * between the points defined by index+1 and index+2.
 */
    public static List<Point3D> Interpolate(List<Point3D> points, int index, int pointsPerSegment, CatmullRomType curveType)
    {
        var result = new List<Point3D>();
        var x = new double[4];
        var y = new double[4];
        var time = new double[4];
        for (var i = 0; i < 4; i++)
        {
            x[i] = points[index + i].X;
            y[i] = points[index + i].Y;
            time[i] = i;
        }

        double tstart = 1;
        double tend = 2;
        if (curveType != CatmullRomType.Uniform)
        {
            double total = 0;
            for (var i = 1; i < 4; i++)
            {
                var dx = x[i] - x[i - 1];
                var dy = y[i] - y[i - 1];
                if (curveType == CatmullRomType.Centripetal)
                {
                    total += Math.Pow(dx * dx + dy * dy, .25);
                }
                else
                {
                    total += Math.Pow(dx * dx + dy * dy, .5);
                }

                time[i] = total;
            }

            tstart = time[1];
            tend = time[2];
        }

        var z1 = 0.0;
        var z2 = 0.0;
        if (!double.IsNaN(points[index + 1].Z))
        {
            z1 = points[index + 1].Z;
        }

        if (!double.IsNaN(points[index + 2].Z))
        {
            z2 = points[index + 2].Z;
        }

        var dz = z2 - z1;
        var segments = pointsPerSegment - 1;
        result.Add(points[index + 1]);
        for (var i = 1; i < segments; i++)
        {
            var xi = Interpolate(x, time, tstart + (i * (tend - tstart)) / segments);
            var yi = Interpolate(y, time, tstart + (i * (tend - tstart)) / segments);
            var zi = z1 + (dz * i) / segments;
            result.Add(new Point3D(xi, yi, zi));
        }

        result.Add(points[index + 2]);
        return result;
    }

    /**
 * Unlike the other implementation here, which uses the default "uniform"
 * treatment of t, this computation is used to calculate the same values but
 * introduces the ability to "parameterize" the t values used in the
 * calculation. This is based on Figure 3 from
 * http://www.cemyuksel.com/research/catmullrom_param/catmullrom.pdf
 *
 * @param p An array of double values of length 4, where interpolation
 * occurs from p1 to p2.
 * @param time An array of time measures of length 4, corresponding to each
 * p value.
 * @param t the actual interpolation ratio from 0 to 1 representing the
 * position between p1 and p2 to interpolate the value.
 * @return
 */
    public static double Interpolate(double[] p, double[] time, double t)
    {
        var l01 = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
        var l12 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
        var l23 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
        var l012 = l01 * (time[2] - t) / (time[2] - time[0]) + l12 * (t - time[0]) / (time[2] - time[0]);
        var l123 = l12 * (time[3] - t) / (time[3] - time[1]) + l23 * (t - time[1]) / (time[3] - time[1]);
        var c12 = l012 * (time[2] - t) / (time[2] - time[1]) + l123 * (t - time[1]) / (time[2] - time[1]);
        return c12;
    }
}