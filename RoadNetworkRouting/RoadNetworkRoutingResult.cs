using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting;

public class RoadNetworkRoutingResult
{
    public QuickGraphSearchResult Route { get; set; }
    public RoadLink[] Links { get; set; }

    public bool Success => Route.Target != null;

    public RoadNetworkRoutingResult(QuickGraphSearchResult route, RoadLink[] links)
    {
        Route = route;
        Links = links;
    }
}