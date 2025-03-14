﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnergyModule.Exceptions;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.IEnumerableExtensions;
using Extensions.Utilities;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Exceptions;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Service;

public abstract class IRoutingService
{
    public string Name { get; protected set; }
    public string Version { get; protected set; }

    public InternalRoutingResponse FromRequest(IList<Point3D> coordinates, RoutingConfig config, CoordinateConverter converter, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        return FromUtm(coordinates.Select(p => converter?.Forward(p) ?? p).ToArray(), config, includeCoordinates, includeLinkReferences, timer, id);
    }

    public InternalRoutingResponse FromUtm(Point3D[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        return FromUtm(coordinates.Select(p => new RoutingPoint(p)).ToArray(), config, includeCoordinates, includeLinkReferences, timer, id);
    }

    public abstract InternalRoutingResponse FromUtm(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null);

    public Task<InternalRoutingResponse> FromRequestAsync(IList<Point3D> coordinates, RoutingConfig config, CoordinateConverter converter, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        return FromUtmAsync(coordinates.Select(p => converter?.Forward(p) ?? p).ToArray(), config, includeCoordinates, includeLinkReferences, timer, id);
    }

    public Task<InternalRoutingResponse> FromUtmAsync(Point3D[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        return FromUtmAsync(coordinates.Select(p => new RoutingPoint(p)).ToArray(), config, includeCoordinates, includeLinkReferences, timer, id);
    }

    public abstract Task<InternalRoutingResponse> FromUtmAsync(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null);
}

public class RoutingService : IRoutingService
{
    private RoutingService()
    {
    }

    private readonly object _lockObject = new();
    public RoadNetworkRouter Router { get; private set; }

    public string NetworkFile { get; private set; }
    public TaskTimer GlobalTimings { get; } = new();
    public DateTime StartedAt { get; private set; }
    public TaskTimer Timings { get; private set; }
    public int TotalRequests { get; private set; }
    public int TotalWaypoints { get; private set; }

    /// <summary>
    /// If set, routing will fail if the sum of the straight line distances between the requested waypoints
    /// is larger than this value.
    /// </summary>
    public double? MaxRouteLengthKm { get; set; } = 1000;

    public override InternalRoutingResponse FromUtm(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        config ??= new RoutingConfig();

        if (MaxRouteLengthKm.HasValue)
        {
            var dist = coordinates.Pairwise().Sum(p => p.A.SearchPoint.DistanceTo(p.B.SearchPoint)) / 1000d;
            if (dist > MaxRouteLengthKm)
                throw new InvalidRouteException($"The requested route is too long (straight line distance is {dist:n2} km, which is higher than the configured maximum of {MaxRouteLengthKm.Value:n2} km).");
        }

        var rs = new InternalRoutingResponse()
        {
            RoutingSource = new RoutingSourceInfo("Built-in router, " + config.Algorithm, Version),
            RequestedWaypoints = new(),
            LinkReferences = includeLinkReferences ? new List<string>() : null,
            Timings = timer ?? new(),
            Links = new(),
            Coordinates = includeCoordinates ? new List<Point3D>() : null
        };

        rs.Timings.Time("routing.init");
        for (var i = 1; i < coordinates.Length; i++)
        {
            var fromCoord = coordinates[i - 1];
            var toCoord = coordinates[i];

            int? ci = includeCoordinates ? rs.Coordinates.Count : null;
            int? lri = includeLinkReferences ? rs.LinkReferences.Count : null;
            rs.RequestedWaypoints.Add(new WayPointData()
            {
                FromWaypoint = fromCoord.SearchPoint,
                ToWaypoint = toCoord.SearchPoint,
                CoordinateIndex = ci,
                LinkReferenceIndex = lri
            });

            rs.Timings.Time("routing.service");

            var path = Router.Search(fromCoord, toCoord, config, rs.Timings);
            //var path = Router.SaveSearchDebugAsGeoJson(fromCoord.SearchPoint, toCoord.SearchPoint, "G:\\Søppel\\2024-01-26 - Entur, routing-debugging\\route_" + id, config, rs.Timings);
            rs.RequestedWaypoints[^1].RoutingInfo = new RoutingInfo(path);

            if (!path.Success) throw new RoutingException($"Couldn't find a route between these points [it={path.Route.InternalData.Iterations}, term={path.Route.InternalData.Termination}].");

            coordinates[i - 1].Update(path.Source);
            coordinates[i].Update(path.Target);
                
            foreach (var link in path.Links)
            {
                if (includeLinkReferences)
                {
                    rs.LinkReferences.Add(link.Reference.ToShortRepresentation());
                }

                rs.Links.Add(link);

                if (includeCoordinates)
                {
                    rs.Coordinates.AddRange(link.Geometry.Select(p => new Point3D(p.X, p.Y, p.Z)));
                }
            }

            rs.Timings.Time("routing.results");
        }
        rs.Timings.Time("routing.service");

        lock (_lockObject)
        {
            GlobalTimings.Append(rs.Timings);
            TotalRequests += 1;
            TotalWaypoints += coordinates.Length;
        }

        return rs;
    }

    public override Task<InternalRoutingResponse> FromUtmAsync(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences, TaskTimer timer = null, string id = null)
    {
        return Task.FromResult(FromUtm(coordinates, config, includeCoordinates, includeLinkReferences, timer, id));
    }

    public IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<string> linkReferences)
    {
        return Router.GetLinksFromReferences(linkReferences);
    }

    public static RoutingService Create(string networkFile, string version = null, string name = null)
    {
        var rs = new RoutingService();
        rs.Version = version;
        rs.Name = name;

        rs.StartedAt = DateTime.Now;
        rs.Timings = new TaskTimer();

        rs.NetworkFile = networkFile;
        rs.Router = RoadNetworkRouter.LoadFrom(networkFile);

        rs.Timings.Time("load.network");
        rs.Router.CreateNearbyLinkLookup();
        rs.Timings.Time("create.nearby");

        return rs;
    }
}