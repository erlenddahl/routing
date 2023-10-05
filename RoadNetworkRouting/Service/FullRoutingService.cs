using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.Utilities;
using Extensions.Utilities.Caching;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Service
{
    public class FullRoutingService
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private FullRoutingService()
        {
        }

        private static readonly object _lockObject = new();
        public static RoadNetworkRouter Router { get; private set; }

        public static string NetworkFile { get; set; }
        public static SkeletonConfig SkeletonConfig { get; set; }
        public static TaskTimer GlobalTimings { get; } = new();
        public static DateTime StartedAt { get; private set; }
        public static TaskTimer Timings { get; private set; }
        public static int TotalRequests { get; set; }
        public static int TotalWaypoints { get; set; }

        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (Router == null)
                {
                    logger.Info("Reading road network");
                    logger.Info("NetworkFile: " + NetworkFile);

                    StartedAt = DateTime.Now;
                    Timings = new TaskTimer();
                    
                    Router = RoadNetworkRouter.LoadFrom(NetworkFile, skeletonConfig: SkeletonConfig);

                    Timings.Time("Loaded network");
                    Router.Graph = Router.CreateGraph();
                    Timings.Time("Created graph");
                    Router.CreateNearbyLinkLookup();
                    Timings.Time("Created nearby lookup");
                }
            }
        }

        public static InternalRoutingResponse FromRequest(IList<Point3D> coordinates, RoutingConfig config, CoordinateConverter converter, bool includeCoordinates, bool includeLinkReferences)
        {
            return FromUtm(coordinates.Select(p => converter?.Forward(p) ?? p).ToArray(), config, includeCoordinates, includeLinkReferences);
        }

        public static InternalRoutingResponse FromUtm(Point3D[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences)
        {
            return FromUtm(coordinates.Select(p => new RoutingPoint(p)).ToArray(), config, includeCoordinates, includeLinkReferences);
        }

        public static InternalRoutingResponse FromUtm(RoutingPoint[] coordinates, RoutingConfig config, bool includeCoordinates, bool includeLinkReferences)
        {
            if (Router == null)
                Initialize();

            var rs = new InternalRoutingResponse()
            {
                RequestedWaypoints = new(),
                LinkReferences = includeLinkReferences ? new List<string>() : null,
                Timings = new(),
                Links = new(),
                Coordinates = includeCoordinates ? new List<Point3D>() : null
            };

            logger.Debug($"Initiating search with {coordinates.Length:n0} waypoints");

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
                
                logger.Debug($"Found {path.Links.Length} links.");

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

            logger.Debug("Finished all");

            return rs;
        }

        public static IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<string> linkReferences)
        {
            Initialize();
            return Router.GetLinksFromReferences(linkReferences);
        }

        public static void Initialize(string networkFile, SkeletonConfig skeletonConfig)
        {
            NetworkFile = networkFile;
            SkeletonConfig = skeletonConfig;

            Initialize();
        }
    }

    public class RoutingPoint
    {
        public Point3D Point { get; }
        public RoadLink Link { get; set; }
        public NearestPointInfo Nearest { get; set; }

        public RoutingPoint(Point3D point)
        {
            Point = point;
        }

        public RoutingPoint(Point3D point, RoadLink roadLink, NearestPointInfo nearest)
        {
            Point = point;
            Link = roadLink;
            Nearest = nearest;
        }

        public void Update(RoutingPoint point)
        {
            Link = point.Link;
            Nearest = point.Nearest;
        }
    }
}