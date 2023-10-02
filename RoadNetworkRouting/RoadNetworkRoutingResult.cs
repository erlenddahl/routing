using System.Linq;
using Extensions.Utilities;
using RoadNetworkRouting.Network;
using RoadNetworkRouting.Service;
using RoadNetworkRouting.Utils;
using Routing;

namespace RoadNetworkRouting;

public class RoadNetworkRoutingResult
{
    private double? _routeDistance;

    public QuickGraphSearchResult Route { get; set; }
    public RoadLink[] Links { get; set; }

    public bool Success => Route.Target != null;

    public RoutingPoint Source { get; set; }
    public RoutingPoint Target { get; set; }

    public double DistanceToSourceVertex => Source.Nearest.DistanceFromLine;
    public double DistanceToTargetVertex => Target.Nearest.DistanceFromLine;
    public TaskTimer Timer { get; }
    public double RouteDistance => _routeDistance ??= Links.Sum(p => p.Length);
    public double TotalDistance => DistanceToSourceVertex + RouteDistance + DistanceToTargetVertex;

    public RoadNetworkRoutingResult(QuickGraphSearchResult route, RoadLink[] links, RoutingPoint source, RoutingPoint target, TaskTimer timer)
    {
        Route = route;
        Links = links;
        Source = source;
        Target = target;
        Timer = timer;
    }
}