using System.Collections.Generic;
using System.Linq;
using RoutingApi.Geometry;

namespace RoutingApi.Helpers
{
    public class RoutingResponse
    {
        public List<LinkReference> LinkReferences { get; set; } = new();
        public List<PointUtm33> Route { get; set; } = new();
        public PointUtm33[] WayPoints { get; set; } = null;
        public List<WayPointIndex> WayPointIndices { get; set; } = new();

        public double SecsModifyLinks { get; set; }
        public double SecsRetrieveLinks { get; set; }
        public double SecsDijkstra { get; set; }

        public static RoutingResponse FromLatLng(LatLng[] coordinates)
        {
            var utmCoordinates = coordinates.Select(p => new PointWgs84(p.Lat, p.Lng).ToUtm33()).ToArray();
            return FromUtm(utmCoordinates);
        }

        public static RoutingResponse FromUtm(PointUtm33[] coordinates)
        {
            return LocalDijkstraRoutingService.FromUtm(coordinates);
        }
    }
}