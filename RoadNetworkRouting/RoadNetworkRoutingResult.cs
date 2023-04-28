using System.Linq;
using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting;

public class RoadNetworkRoutingResult
{
    private double? _routeDistance;

    public QuickGraphSearchResult Route { get; set; }
    public RoadLink[] Links { get; set; }

    public bool Success => Route.Target != null;

    public double DistanceToSourceVertex { get; set; }
    public double DistanceToTargetVertex { get; set; }
    public double RouteDistance => _routeDistance ??= Links.Sum(p => p.Geometry.Length);
    public double TotalDistance => DistanceToSourceVertex + RouteDistance + DistanceToTargetVertex;

    public RoadNetworkRoutingResult(QuickGraphSearchResult route, RoadLink[] links, double distanceToSourceVertex, double distanceToTargetVertex)
    {
        Route = route;
        Links = links;
        DistanceToSourceVertex = distanceToSourceVertex;
        DistanceToTargetVertex = distanceToTargetVertex;
    }
}