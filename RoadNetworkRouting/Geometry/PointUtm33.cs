using System;
using System.Collections.Generic;
using System.Globalization;
using EnergyModule.Geometry.SimpleStructures;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace RoadNetworkRouting.Geometry
{
    public class PointUtm33 : Point3D
    {
        public override string ToString(){
            return X.ToString("n2") + ", " + Y.ToString("n2") + ", " + Z.ToString("n1");
        }

        public PointUtm33():base(0,0,0)
        {

        }

        public PointUtm33(double x, double y, double z):base(x,y,z)
        {
        }

        internal static PointUtm33 FromWkt(string srid, string[] p)
        {
            if(srid != "32633")
                throw new Exception("WKT is not UTM33 (32633): " + srid);

            return new PointUtm33()
            {
                X = double.Parse(p[0], CultureInfo.InvariantCulture),
                Y = double.Parse(p[1], CultureInfo.InvariantCulture),
                Z = p.Length > 2 ? double.Parse(p[2], CultureInfo.InvariantCulture) : 0
            };
        }

        public double DistanceTo(PointUtm33 anotherPoint)
        {
            return Math.Sqrt(Math.Pow(X - anotherPoint.X, 2) + Math.Pow(Y - anotherPoint.Y, 2));
        }


        private static readonly CoordinateTransformationFactory _ctfac = new CoordinateTransformationFactory();
        private static readonly GeographicCoordinateSystem _wgs84 = GeographicCoordinateSystem.WGS84;
        private static readonly ProjectedCoordinateSystem _utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        private static readonly ICoordinateTransformation _transUtmToWgs = _ctfac.CreateFromCoordinateSystems(_utm33, _wgs84);
        private static readonly ICoordinateTransformation _transWgsToUtm = _ctfac.CreateFromCoordinateSystems(_wgs84, _utm33);

        public PointWgs84 ToWgs84()
        {
            return ToWgs84(this);
        }

        public static PointWgs84 ToWgs84(Point3D point)
        {
            var pUtm = _transUtmToWgs.MathTransform.Transform(new[] { point.X, point.Y });

            return new PointWgs84() { Lat = pUtm[1], Lng = pUtm[0], Alt = point.Z };
        }

        public static PointWgs84 Utm33ToWgs84(double x, double y, double z)
        {
            return new PointUtm33(x, y, z).ToWgs84();
        }

        public static IEnumerable<PointUtm33> Fill(List<PointUtm33> points, int maxDist)
        {
            for(var i = 0; i < points.Count - 1; i++)
            {
                var curr = points[i];
                yield return curr;

                while (curr.DistanceTo(points[i + 1]) > maxDist)
                {
                    curr = new PointUtm33(curr.X, curr.Y, curr.Z);
                    curr.MoveTowards(points[i + 1], maxDist);
                    yield return curr;
                }
            }
        }

        private void MoveTowards(PointUtm33 point, int distanceToMove)
        {
            var dist = DistanceTo(point);
            var fac = distanceToMove / dist;

            X += (point.X - X) * fac;
            Y += (point.Y - Y) * fac;
        }

        public bool Identical(PointUtm33 point)
        {
            return Math.Abs(X - point.X) < 0.001 && Math.Abs(Y - point.Y) < 0.001;
        }

        public static double Distance(IEnumerable<PointUtm33> route)
        {
            var dist = 0d;
            PointUtm33 prev = null;

            foreach(var coord in route)
            {
                if (prev != null) 
                    dist += prev.DistanceTo(coord);
                prev = coord;
            }

            return dist;
        }

        public bool IdenticalXY(PointUtm33 anotherPoint)
        {
            return X == anotherPoint.X && Y == anotherPoint.Y;
        }
    }

}