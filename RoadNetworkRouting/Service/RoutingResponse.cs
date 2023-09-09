using System;
using System.Linq;
using EnergyModule.Geometry;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Service;

public class RoutingResponse : InternalRoutingResponse
{
    /// <summary>
    /// The full length of the route, measured in meters.
    /// </summary>
    public double DistanceM { get; set; }

    /// <summary>
    /// Contains a compressed list of 2D coordinates with the given maximum number of decimals (if requested).
    /// </summary>
    public double[][] CompressedCoordinates { get; set; }

    /// <summary>
    /// Contains the config object that was used in the routing.
    /// </summary>
    public RoutingConfig RoutingConfig { get; set; }

    /// <summary>
    /// Contains the response definition that was used to create this response.
    /// </summary>
    public RoutingResponseDefinition ResponseDefinition { get; set; }

    /// <summary>
    /// The SRID of the input coordinates.
    /// </summary>
    public int SourceSrid { get; set; }

    /// <summary>
    /// The SRID of the returned coordinates.
    /// </summary>
    public int OutputSrid { get; set; }

    /// <summary>
    /// Contains an error message if anything failed during this calculation. The rest of the object will then be empty.
    /// </summary>
    public string ErrorMessage { get; set; }

    public RoutingResponse(RoutingRequest request, InternalRoutingResponse result)
    {
        var r = request.Response ?? new RoutingResponseDefinition() { Coordinates = true };
        var converter = CoordinateConverter.FromUtm33(r.OutputSrid);

        var returnedCoordinates = result.Coordinates.Select(converter.Forward).ToList();

        DistanceM = LineTools.CalculateLength(result.Coordinates);
        SourceSrid = request.SourceSrid;
        OutputSrid = r.OutputSrid;

        if (r.EchoResponseDefinition)
            ResponseDefinition = r;
        if (r.RoutingConfig)
            RoutingConfig = request.RoutingConfig ?? new RoutingConfig();
        if (r.Coordinates)
            Coordinates = returnedCoordinates;
        if (r.CompressedCoordinates)
            CompressedCoordinates = returnedCoordinates.Select(p => new[] { Math.Round(p.X, r.CompressedCoordinatesNumberOfDecimals), Math.Round(p.Y, r.CompressedCoordinatesNumberOfDecimals) }).ToArray();
        if (r.LinkReferences)
            LinkReferences = result.LinkReferences;
        if (r.Links)
            Links = result.Links.Select(p => p.ConvertCoordinates(converter)).ToList();
        if (r.RequestedWaypoints)
            RequestedWaypoints = result.RequestedWaypoints.Select(p => p.ConvertCoordinates(converter)).ToList();
        if (r.Timings)
            Timings = result.Timings;
    }

    public RoutingResponse(Exception ex)
    {
        ErrorMessage = ex.Message;
    }
}