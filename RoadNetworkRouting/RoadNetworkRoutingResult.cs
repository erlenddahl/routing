using Routing;

namespace RoadNetworkRouting;

public class RoadNetworkRoutingResult
{
    public QuickGraphSearchResult Route { get; set; }
    public GdbRoadLinkData[] Links { get; set; }

    public bool Success => Route.Target != null;

    public RoadNetworkRoutingResult(QuickGraphSearchResult route, GdbRoadLinkData[] links)
    {
        Route = route;
        Links = links;
    }
}