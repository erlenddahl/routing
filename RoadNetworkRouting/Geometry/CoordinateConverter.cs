using System;
using System.Collections.Generic;
using EnergyModule.Fuel;
using EnergyModule.Geometry.SimpleStructures;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace RoadNetworkRouting.Geometry
{
    public class CoordinateConverter
    {
        public int FromSrid { get; }
        public int ToSrid { get; }
        
        private static readonly CoordinateTransformationFactory _ctfac = new CoordinateTransformationFactory();
        private readonly ICoordinateTransformation _sourceToTarget;
        private readonly ICoordinateTransformation _targetToSource;

        private static Dictionary<(int from, int to), CoordinateConverter> _cachedConverters = new();

        public CoordinateConverter(int fromSrid, int toSrid)
        {
            FromSrid = fromSrid;
            ToSrid = toSrid;
            var source = ProjNet.SRID.SRIDReader.GetCSbyID(fromSrid);
            var target = ProjNet.SRID.SRIDReader.GetCSbyID(toSrid);
            _sourceToTarget = _ctfac.CreateFromCoordinateSystems(source, target);
            _targetToSource = _ctfac.CreateFromCoordinateSystems(target, source);
        }

        /// <summary>
        /// Converts the given point from the source coordinate system to the target coordinate system.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point3D Forward(Point3D point)
        {
            return Forward(point.X, point.Y, point.Z);
        }

        /// <summary>
        /// Converts the given point from the source coordinate system to the target coordinate system.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Point3D Forward(double x, double y, double z = 0)
        {
            var pUtm = _sourceToTarget.MathTransform.Transform(new[] { x, y });
            return new PointUtm33() { X = pUtm[0], Y = pUtm[1], Z = z };
        }

        /// <summary>
        /// Converts the given point from the target coordinate system to the source coordinate system.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point3D Backwards(Point3D point)
        {
            var pUtm = _targetToSource.MathTransform.Transform(new[] { point.X, point.Y });
            return new PointUtm33() { X = pUtm[0], Y = pUtm[1], Z = point.Z };
        }

        /// <summary>
        /// Creates a converter that transforms from the sourceSrid to
        /// UTM33N/EPSG:32633.
        /// </summary>
        /// <param name="sourceSrid"></param>
        /// <returns></returns>
        public static CoordinateConverter ToUtm33(int sourceSrid)
        {
            return CreateOrGetCached(sourceSrid, 32633);
        }

        /// <summary>
        /// Creates a converter that transforms from the sourceSrid to
        /// WGS84/EPSG:4326.
        /// </summary>
        /// <param name="sourceSrid"></param>
        /// <returns></returns>
        public static CoordinateConverter ToWgs84(int sourceSrid)
        {
            return CreateOrGetCached(sourceSrid, 4326);
        }
        /// <summary>
        /// Creates a converter that transforms from UTM33N/EPSG:32633 to
        /// the targetSrid.
        /// </summary>
        /// <param name="targetSrid"></param>
        /// <returns></returns>
        public static CoordinateConverter FromUtm33(int targetSrid)
        {
            return CreateOrGetCached(32633, targetSrid);
        }

        public static CoordinateConverter CreateOrGetCached(int from, int to)
        {
            lock (_cachedConverters)
            {
                if (_cachedConverters.TryGetValue((from, to), out var c)) return c;
                c = new CoordinateConverter(from, to);
                _cachedConverters.Add((from, to), c);
                return c;
            }
        }
    }
}