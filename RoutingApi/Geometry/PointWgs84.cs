using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace RoutingApi.Geometry
{
    public class PointWgs84
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Alt { get; set; }

        public PointWgs84()
        {

        }

        public PointWgs84(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }

        public override string ToString(){
            return Lat.ToString("n5") + ", " + Lng.ToString("n5") + ", " + Alt.ToString("n1");
        }

        internal static PointWgs84 FromWkt(string srid, string[] p)
        {
            if (srid != "4326")
                throw new Exception("WKT is not WGS84 (4326): " + srid);

            return new PointWgs84()
            {
                Lat = double.Parse(p[0], CultureInfo.InvariantCulture),
                Lng = double.Parse(p[1], CultureInfo.InvariantCulture),
                Alt = double.Parse(p[2], CultureInfo.InvariantCulture)
            };
        }

        private static readonly CoordinateTransformationFactory _ctfac = new CoordinateTransformationFactory();
        private static readonly GeographicCoordinateSystem _wgs84 = GeographicCoordinateSystem.WGS84;
        private static readonly ProjectedCoordinateSystem _utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        private static readonly ICoordinateTransformation _transUtmToWgs = _ctfac.CreateFromCoordinateSystems(_utm33, _wgs84);
        private static readonly ICoordinateTransformation _transWgsToUtm = _ctfac.CreateFromCoordinateSystems(_wgs84, _utm33);

        public PointUtm33 ToUtm33()
        {
            var pUtm = _transWgsToUtm.MathTransform.Transform(new[] { Lng, Lat });

            return new PointUtm33() { X = pUtm[0], Y = pUtm[1], Z = Alt };
        }
    }

}