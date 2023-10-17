using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Service;

public class SingleRoutingRequest : RoutingRequest
{
    /// <summary>
    /// A list of at least two waypoints to find a route between.
    /// </summary>
    public Point3D[] Waypoints { get; set; }

    public static SingleRoutingRequest From(RoutingRequest source, Point3D[] waypoints)
    {
        return new SingleRoutingRequest()
        {
            RoutingConfig = source.RoutingConfig,
            SourceSrid = source.SourceSrid,
            OutputSrid = source.OutputSrid,
            Response = source.Response,
            Waypoints = waypoints
        };
    }

    public RoutingResponse Route(RoutingService service)
    {
        try
        {
            if (Waypoints == null || Waypoints.Length < 2) throw new Exception("Each route must have at least two coordinates.");

            var converter = CoordinateConverter.ToUtm33(SourceSrid);
            var result = service.FromRequest(Waypoints, RoutingConfig, converter, Response.Coordinates || Response.CompressedCoordinates, Response.LinkReferences);

            return new RoutingResponse(this, result);
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#endif
            return new RoutingResponse(ex);
        }
    }
}
public class MultiRoutingRequest : RoutingRequest
{
    /// <summary>
    /// A list of multiple requests, where each request is a list of at least two waypoints to find a route between.
    /// </summary>
    public Point3D[][] Waypoints { get; set; }

    public IEnumerable<RoutingResponse> Route(RoutingService service)
    {
        var ix = 0;
        return Waypoints.Select(p => new
            {
                sequence = ix++,
                request = p
            })
            .AsParallel()
            .Select(p => new
            {
                p.sequence, result = SingleRoutingRequest.From(this, p.request).Route(service)
            })
            .OrderBy(p => p.sequence)
            .Select(p => p.result);
    }
}
public class MatrixRoutingRequest : RoutingRequest
{
    /// <summary>
    /// A list of multiple waypoints, where there will be found a route from each waypoint to all other waypoints.
    /// </summary>
    public Point3D[] Waypoints { get; set; }

    public IEnumerable<RoutingResponse[]> Route(RoutingService service)
    {
        var ix = 0;
        return Waypoints.Select(p => new
            {
                sequence = ix++,
                source = p
            })
            .AsParallel()
            .Select(p => new { p.sequence, results = RouteOneToAll(service, p.source, Waypoints).ToArray() })
            .OrderBy(p => p.sequence)
            .Select(p => p.results);
    }

    public IEnumerable<RoutingResponse> RouteOneToAll(RoutingService service, Point3D source, Point3D[] targets)
    {
        //TODO: Optimized by running OneToAll routing instead of looping.
        foreach (var target in targets)
        {
            yield return SingleRoutingRequest.From(this, new[] { source, target }).Route(service);
        }
    }
}

public abstract class RoutingRequest
{
    /// <summary>
    /// The configuration to be used by the routing engine.
    /// </summary>
    public RoutingConfig RoutingConfig { get; set; }

    /// <summary>
    /// A definition of what to include in the output.
    /// </summary>
    public RoutingResponseDefinition Response { get; set; }

    /// <summary>
    /// The SRID of the incoming coordinates.
    /// Use OutputSrid on the Response definition to select which SRID the returned coordinates should use.
    /// </summary>
    public int SourceSrid { get; set; } = 32633;

    /// <summary>
    /// The SRID used in coordinates on the response.
    /// </summary>
    public int OutputSrid { get; set; } = 32633;
}