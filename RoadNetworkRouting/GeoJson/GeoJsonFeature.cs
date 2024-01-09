using System.Collections.Generic;
using System.Linq;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.GeoJson
{
    public class GeoJsonFeature : GeoJsonObject
    {
        public GeoJsonObject Geometry { get; set; }
        public object Properties { get; set; }

        public GeoJsonFeature() : base("Feature")
        {
        }

        public override string ToString()
        {
            return GeoJsonCollection.Serialize(this);
        }

        public static GeoJsonFeature LineString(IEnumerable<Point3D> coordinates, int sourceSrid, object properties = null)
        {
            return LineString(coordinates, CoordinateConverter.ToWgs84(sourceSrid), properties);
        }

        public static GeoJsonFeature LineString(IEnumerable<Point3D> coordinates, CoordinateConverter converter, object properties = null)
        {
            return LineString(coordinates.Select(converter.Forward), properties);
        }

        public static GeoJsonFeature LineString(IEnumerable<Point3D> coordinates, object properties = null)
        {
            return LineString(coordinates.Select(p => new[] { p.X, p.Y }), properties);
        }

        public static GeoJsonFeature LineString(IEnumerable<double[]> coordinates, object properties = null)
        {
            return new GeoJsonFeature()
            {
                Geometry = new GeoJsonLine()
                {
                    Coordinates = coordinates.ToArray()
                },
                Properties = properties
            };
        }

        public static GeoJsonFeature Polygon(IEnumerable<Point3D> coordinates, int sourceSrid, object properties = null)
        {
            return Polygon(coordinates, CoordinateConverter.ToWgs84(sourceSrid), properties);
        }

        public static GeoJsonFeature Polygon(IEnumerable<Point3D> coordinates, CoordinateConverter converter, object properties = null)
        {
            return Polygon(coordinates.Select(converter.Forward), properties);
        }

        public static GeoJsonFeature Polygon(IEnumerable<Point3D> coordinates, object properties = null)
        {
            return Polygon(coordinates.Select(p => new[] { p.X, p.Y }), properties);
        }

        public static GeoJsonFeature Polygon(IEnumerable<double[]> coordinates, object properties = null)
        {
            return new GeoJsonFeature()
            {
                Geometry = new GeoJsonPolygon()
                {
                    Coordinates = new[] { coordinates.ToArray() }
                },
                Properties = properties
            };
        }

        public static GeoJsonFeature Point(Point3D point, int sourceSrid, object properties = null)
        {
            return Point(point, CoordinateConverter.ToWgs84(sourceSrid), properties);
        }

        public static GeoJsonFeature Point(double x, double y, int sourceSrid, object properties = null)
        {
            return Point(new Point3D(x, y), CoordinateConverter.ToWgs84(sourceSrid), properties);
        }

        public static GeoJsonFeature Point(Point3D point, CoordinateConverter converter, object properties = null)
        {
            return Point(converter.Forward(point), properties);
        }

        public static GeoJsonFeature Point(double x, double y, CoordinateConverter converter, object properties = null)
        {
            return Point(converter.Forward(new Point3D(x, y)), properties);
        }

        public static GeoJsonFeature Point(Point3D point, object properties = null)
        {
            return Point(point.X, point.Y, properties);
        }

        public static GeoJsonFeature Point(double x, double y, object properties = null)
        {
            return new GeoJsonFeature()
            {
                Geometry = new GeoJsonPoint()
                {
                    Coordinates = new[] { x, y }
                },
                Properties = properties
            };
        }
    }
}
