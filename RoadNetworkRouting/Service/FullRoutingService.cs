using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.Utilities;
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
        private static RoadNetworkRouter _router;

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
                if (_router == null)
                {
                    logger.Info("Reading road network");
                    logger.Info("NetworkFile: " + NetworkFile);

                    StartedAt = DateTime.Now;
                    Timings = new TaskTimer();
                    
                    _router = RoadNetworkRouter.LoadFrom(NetworkFile, skeletonConfig: SkeletonConfig);

                    Timings.Time("Loaded network");
                    _router.Graph = _router.CreateGraph();
                    Timings.Time("Created graph");
                    _router.CreateNearbyLinkLookup();
                    Timings.Time("Created nearby lookup");
                }
            }
        }

        public static InternalRoutingResponse FromRequest(IList<Point3D> coordinates, RoutingConfig config, CoordinateConverter converter)
        {
            return FromUtm(coordinates.Select(p => converter.Forward(p)).ToArray());
        }

        public static InternalRoutingResponse FromUtm(Point3D[] coordinates, RoutingConfig config = null)
        {
            if (_router == null)
                Initialize();

            var rs = new InternalRoutingResponse()
            {
                RequestedWaypoints = new(),
                LinkReferences = new(),
                Timings = new(),
                Links = new(),
                Coordinates = new()
            };

            logger.Debug($"Initiating search with {coordinates.Length:n0} waypoints");

            for (var i = 1; i < coordinates.Length; i++)
            {
                var fromCoord = coordinates[i - 1];
                var toCoord = coordinates[i];

                rs.RequestedWaypoints.Add(new WayPointData()
                {
                    FromWaypoint = fromCoord,
                    ToWaypoint = toCoord,
                    CoordinateIndex = rs.Coordinates.Count,
                    LinkReferenceIndex = rs.LinkReferences.Count
                });

                var path = _router.Search(fromCoord, toCoord, config);

                if (!path.Success) throw new Exception("Couldn't find a route between these points.");

                logger.Debug($"Found {path.Links.Length} links.");

                foreach (var link in path.Links)
                {
                    rs.LinkReferences.Add(link.Reference.ToShortRepresentation());
                    rs.Links.Add(link);
                    rs.Coordinates.AddRange(link.Geometry.Select(p => new Point3D(p.X, p.Y, p.Z)));
                }

                rs.Timings.Append(path.Timer);

                logger.Debug("Finished " + i);
            }

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
            return _router.GetLinksFromReferences(linkReferences);
        }

        public static void Initialize(string networkFile, SkeletonConfig skeletonConfig)
        {
            NetworkFile = networkFile;
            SkeletonConfig = skeletonConfig;

            Initialize();
        }
    }
}