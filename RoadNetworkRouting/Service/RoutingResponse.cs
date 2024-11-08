using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Service;

public class RoutingResponse : InternalRoutingResponse
{
    private readonly Exception _ex;

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

    public RoutingResponse(){}

    public RoutingResponse(RoutingRequest request, InternalRoutingResponse result)
    {
        var r = request.Response ?? new RoutingResponseDefinition() { Coordinates = true };
        var converter = CoordinateConverter.FromUtm33(request.OutputSrid);

        var returnedCoordinates = GetCoordinatesIfNeeded(r, result, converter);

        DistanceM = result.Links.Sum(p => p.Length);
        SourceSrid = request.SourceSrid;
        OutputSrid = request.OutputSrid;

        RoutingSource = result.RoutingSource;

        if (r.RoutingConfig)
            RoutingConfig = request.RoutingConfig ?? new RoutingConfig();
        if (r.Coordinates)
            Coordinates = returnedCoordinates;
        if (r.CompressedCoordinates)
            CompressedCoordinates = returnedCoordinates?.Select(p => new[] { Math.Round(p.X, r.CompressedCoordinatesNumberOfDecimals), Math.Round(p.Y, r.CompressedCoordinatesNumberOfDecimals) }).ToArray();
        if (r.LinkReferences)
            LinkReferences = result.LinkReferences;
        if (r.Links)
            Links = result.Links.Select(p => p.ConvertCoordinates(converter)).ToList();
        if (r.RequestedWaypoints)
            RequestedWaypoints = result.RequestedWaypoints.Select(p => p.ConvertCoordinates(converter)).ToList();
        if (r.Timings)
            Timings = result.Timings;
    }

    private List<Point3D> GetCoordinatesIfNeeded(RoutingResponseDefinition r, InternalRoutingResponse result, CoordinateConverter converter)
    {
        if (!r.Coordinates && !r.CompressedCoordinates) return null;

        return (result.Coordinates ?? result.Links.SelectMany(p => p.Geometry))?.Select(converter.Forward).ToList();
    }

    public RoutingResponse(Exception ex)
    {
        ErrorMessage = ex.Message;
        _ex = ex;
    }

    public RoutingResponse CheckThrow()
    {
        if (_ex != null) throw _ex;
        return this;
    }
}