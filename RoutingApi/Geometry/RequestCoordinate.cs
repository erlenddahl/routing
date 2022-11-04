using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace RoutingApi.Geometry
{
    public class RequestCoordinate
    {
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        
        public PointUtm33 GetUtm33()
        {
            if (X.HasValue && Y.HasValue) return new PointUtm33(X.Value, Y.Value, 0);
            if (Lat.HasValue && Lng.HasValue) return new PointWgs84(Lat.Value, Lng.Value).ToUtm33();
            throw new Exception("A coordinate must be given with either X and Y (UTM33) or Lat and Lng (WGS84).");
        }
    }
}