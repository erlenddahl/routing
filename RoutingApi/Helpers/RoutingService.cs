using System.Collections.Generic;
using System.Linq;
using RoutingApi.Geometry;

namespace RoutingApi.Helpers
{
    public class RoutingService
    {
        public List<LinkReference> LinkReferences { get; set; } = new List<LinkReference>();
        public List<PointUtm33> Route { get; set; } = new List<PointUtm33>();
        public List<PointUtm33> WayPoints { get; set; } = new List<PointUtm33>();
        public List<WayPointIndex> WayPointIndices { get; set; } = new List<WayPointIndex>();

        public double SecsModifyLinks { get; set; }
        public double SecsRetrieveLinks { get; set; }
        public double SecsDijkstra { get; set; }

        public static RoutingService FromLatLng(List<LatLng> coordinates)
        {
            var utmCoordinates = coordinates.Select(p => new PointWgs84(p.Lat, p.Lng).ToUtm33()).ToList();
            return FromUtm(utmCoordinates);
        }

        public static RoutingService FromUtm(List<PointUtm33> coordinates)
        {
            return LocalDijkstraRoutingService.FromUtm(coordinates);
        }
    }

    public class WayPointIndex
    {
        public int CoordinateIndex { get; set; }
        public int LinkReferenceIndex { get; set; }
    }
}