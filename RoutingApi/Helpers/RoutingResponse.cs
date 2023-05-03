using System.Collections.Generic;
using System.Linq;
using EnergyModule.Network;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Network;
using RoadNetworkRouting.Utils;
using RoutingApi.Geometry;

namespace RoutingApi.Helpers
{
    public class RoutingResponse
    {
        public List<LinkReference> LinkReferences { get; set; } = new();
        public List<RoadLink> Links { get; set; } = new();
        public List<PointUtm33> Route { get; set; } = new();
        public PointUtm33[] WayPoints { get; set; } = null;
        public List<WayPointIndex> WayPointIndices { get; set; } = new();

        public RoutingTimer Timings { get; set; } = new();

        public static RoutingResponse FromRequestCoordinates(IList<RequestCoordinate> coordinates, RoutingConfig config = null)
        {
            var utmCoordinates = coordinates.Select(p => p.GetUtm33()).ToArray();
            return FromUtm(utmCoordinates,config);
        }

        public static RoutingResponse FromUtm(PointUtm33[] coordinates, RoutingConfig config = null)
        {
            return LocalDijkstraRoutingService.FromUtm(coordinates, config);
        }

        public static IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<LinkReference> linkReferences)
        {
            return LocalDijkstraRoutingService.GetLinksFromReferences(linkReferences);
        }
    }
}