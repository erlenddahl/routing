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

    /// <summary>
    /// If set to false, elevation data will not be retrieved, resulting in a 2D route. This will greatly reduce the routing time for
    /// datasets where elevation data is fetched from a secondary source (such as the OSRM+OTD router).
    /// </summary>
    public bool IncludeElevation { get; set; } = true;

    /// <summary>
    /// If set to false, the elevation cache will not be used. This means the request will take longer to complete, since elevation data
    /// has to be retrieved for all points.
    /// </summary>
    public bool IgnoreElevationCache { get; set; } = true;

    /// <summary>
    /// Defines the radius for a median filter for smoothing the elevation values. Set it to null for no smoothing.
    /// </summary>
    public double? ElevationSmoothingWindowSize { get; set; } = 10;
}