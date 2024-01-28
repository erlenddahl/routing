using Routing;

namespace RoadNetworkRouting.Service;

public class RoutingInfo
{
    /// <summary>
    /// The length of the found route (the drivable part).
    /// </summary>
    public double RouteDistance { get; set; }

    /// <summary>
    /// The distance from the source point to where the route begins.
    /// </summary>
    public double DistanceToSourceVertex { get; set; }

    /// <summary>
    /// The distance from where the route ends to the target point.
    /// </summary>
    public double DistanceToTargetVertex { get; set; }

    /// <summary>
    /// The total length of the route (<see cref="RouteDistance"/> + <see cref="DistanceToSourceVertex"/> + <see cref="DistanceToTargetVertex"/>).
    /// </summary>
    public double TotalDistance { get; set; }

    /// <summary>
    /// True if the routing worked.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The number of nodes that were skipped because the cumulative cost was above the given maximum cost.
    /// </summary>
    public int AboveMaxCost { get; set; }

    /// <summary>
    /// The total number of iterations (scanned vertices).
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// What ended the routing.
    /// </summary>
    public TerminationType Termination { get; set; }

    /// <summary>
    /// The total duration of the routing operation.
    /// </summary>
    public double ElapsedTimeMs { get; set; }

    public RoutingInfo(){}

    public RoutingInfo(RoadNetworkRoutingResult path)
    {
        RouteDistance = path.RouteDistance;
        TotalDistance = path.TotalDistance;
        DistanceToSourceVertex = path.DistanceToSourceVertex;
        DistanceToTargetVertex = path.DistanceToTargetVertex;
        Success = path.Success;
        AboveMaxCost = path.Route.InternalData.AboveMaxCost;
        Iterations = path.Route.InternalData.Iterations;
        Termination = path.Route.InternalData.Termination;
        ElapsedTimeMs = path.Route.InternalData.ElapsedTimeMs;
    }
}