namespace RoadNetworkRouting.Service;

/// <summary>
/// Used to define the contents of the calculation response.
/// </summary>
public class RoutingResponseDefinition
{
    /// <summary>
    /// Include the list of requested waypoints, with indices pointing into
    /// the Coordinates and LinkReferences arrays in case you need to extract
    /// parts of these arrays for certain waypoints.
    /// </summary>
    public bool RequestedWaypoints { get; set; }

    /// <summary>
    /// Include the full configuration object used in the routing.
    /// </summary>
    /// <example>true</example>
    public bool RoutingConfig { get; set; }

    /// <summary>
    /// Include all road links in the route.
    /// </summary>
    /// <example>true</example>
    public bool Links { get; set; }

    /// <summary>
    /// Include a list of link references for the route that was found.
    /// </summary>
    /// <example>false</example>
    public bool LinkReferences { get; set; } 

    /// <summary>
    /// Include the full list of 3D coordinates.
    /// </summary>
    public bool Coordinates { get; set; } 

    /// <summary>
    /// Include a list of rounded 2D coordinates for map visualization.
    /// </summary>
    public bool CompressedCoordinates { get; set; }

    /// <summary>
    /// The number of decimals to include in the compressed coordinates list.
    /// </summary>
    public int CompressedCoordinatesNumberOfDecimals { get; set; } = 5;

    /// <summary>
    /// Include timing data for the routing.
    /// </summary>
    public bool Timings { get; set; }
}