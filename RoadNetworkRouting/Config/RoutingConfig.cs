using RoadNetworkRouting.Exceptions;

namespace RoadNetworkRouting.Config;

public class RoutingConfig
{
    /// <summary>
    /// How to handle situations where the entry points into the road network from the source
    /// and target locations are in different groups of a disconnected road network.
    /// </summary>
    public GroupHandling DifferentGroupHandling { get; set; } = GroupHandling.BestGroup;

    /// <summary>
    /// The initial radius for picking links to check when searching for the nearest link (the
    /// entry point) for source and target locations.
    /// </summary>
    public int InitialSearchRadius { get; set; } = 100;

    /// <summary>
    /// How the search <see cref="InitialSearchRadius"/> will grow if the initial search fails.
    /// For each failed search, the radius will be multiplied with this number.
    /// </summary>
    public int SearchRadiusIncrement { get; set; } = 10;

    /// <summary>
    /// If the link search reaches this radius without finding a link, a <see cref="NoLinksFoundException"/> will be thrown./>
    /// </summary>
    public int MaxSearchRadius { get; set; } = 1000;

    /// <summary>
    /// Which routing algorithm to use. AStar is faster and probably optimal, while Dijkstra is guaranteed to be optimal.
    /// </summary>
    public RoutingAlgorithm Algorithm { get; set; } = RoutingAlgorithm.AStar;

    /// <summary>
    /// If the routing takes longer time than this, it will be cancelled.
    /// When using A*, the currently best route will be returned.
    /// When using Dijkstra, an error will be thrown.
    /// </summary>
    public double MaxSearchDurationMs { get; set; } = 15_000;
}