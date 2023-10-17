using System;
using System.Collections.Generic;
using System.Linq;
using EnergyModule.Exceptions;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.IEnumerableExtensions;
using Extensions.Utilities;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Service;

public class RoutingService
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

    public InternalRoutingResponse FromRequest(IList<Point3D> coordinates, RoutingConfig config, CoordinateConverter converter, bool includeCoordinates, bool includeLinkReferences)
    {
        return FromUtm(coordinates.Select(p => converter?.Forward(p) ?? p).ToArray(), config, includeCoordinates, includeLinkReferences);
    }

    public InternalRoutingResponse FromUtm(Point3D[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences)
    {
        return FromUtm(coordinates.Select(p => new RoutingPoint(p)).ToArray(), config, includeCoordinates, includeLinkReferences);
    }

    public InternalRoutingResponse FromUtm(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences)
    {
        if (MaxRouteLengthKm.HasValue)
        {
            var dist = coordinates.Pairwise().Sum(p => p.A.Point.DistanceTo(p.B.Point)) / 1000d;
            if (dist > MaxRouteLengthKm)
                throw new InvalidRouteException($"The requested route is too long (straight line distance is {dist:n2}, which is higher than the configured maximum of {MaxRouteLengthKm.Value:n2}).");
        }

        var rs = new InternalRoutingResponse()
        {
            RequestedWaypoints = new(),
            LinkReferences = includeLinkReferences ? new List<string>() : null,
            Timings = new(),
            Links = new(),
            Coordinates = includeCoordinates ? new List<Point3D>() : null
        };

        rs.Timings.Time("routing.init");
        for (var i = 1; i < coordinates.Length; i++)
        {
            var fromCoord = coordinates[i - 1];
            var toCoord = coordinates[i];

            var ci = includeCoordinates ? rs.Coordinates.Count : -1;
            var lri = includeLinkReferences ? rs.LinkReferences.Count : -1;
            rs.RequestedWaypoints.Add(new WayPointData()
            {
                FromWaypoint = fromCoord.Point,
                ToWaypoint = toCoord.Point,
                CoordinateIndex = ci,
                LinkReferenceIndex = lri
            });

            rs.Timings.Time("routing.service");

            var path = Router.Search(fromCoord, toCoord, config, rs.Timings);
            if (!path.Success) throw new Exception("Couldn't find a route between these points.");
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

    public IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<string> linkReferences)
    {
        return Router.GetLinksFromReferences(linkReferences);
    }

    public static RoutingService Create(string networkFile)
    {
        var rs = new RoutingService();
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