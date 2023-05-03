using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotSpatial.Topology;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using Extensions.IEnumerableExtensions;
using Extensions.Utilities;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using RoadNetworkRouting;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Network;
using RoadNetworkRouting.Utils;
using Routing;
using RoutingApi.Geometry;

namespace RoutingApi.Helpers
{
    public class LocalDijkstraRoutingService
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private LocalDijkstraRoutingService()
        {
        }

        private static readonly object _lockObject = new();
        private static RoadNetworkRouter _router;

        public static string NetworkFile { get; set; }
        public static SkeletonConfig SkeletonConfig { get; set; }
        public static RoutingTimer GlobalTimings { get; } = new ();
        public static DateTime StartedAt { get; private set; }
        public static DateTime NetworkLoadedAt { get; private set; }
        public static DateTime GraphCreatedAt { get; private set; }
        public static DateTime NearbyLookupCreatedAt { get; private set; }
        public static int TotalRequests { get; set; }
        public static int TotalWaypoints { get; set; }

        public static RoutingResponse FromLatLng(List<RequestCoordinate> coordinates, RoutingConfig config = null)
        {
            var utmCoordinates = coordinates.Select(p => p.GetUtm33()).ToArray();
            return FromUtm(utmCoordinates, config);
        }

        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_router == null)
                {
                    logger.Info("Reading road network");
                    StartedAt = DateTime.Now;
                    _router = RoadNetworkRouter.LoadFrom(NetworkFile, skeletonConfig: SkeletonConfig);
                    NetworkLoadedAt = DateTime.Now;
                    logger.Info("Read road network (" + _router.Links.Count.ToString("n0") + " links)");
                    GraphCreatedAt = DateTime.Now;
                    logger.Info("Created graph (" + _router.Graph.EdgeCount.ToString("n0") + " edges)");
                    _router.CreateNearbyLinkLookup();
                    NearbyLookupCreatedAt = DateTime.Now;
                    logger.Info("Created nearby lookup");
                }
            }
        }

        public static RoutingResponse FromUtm(PointUtm33[] coordinates, RoutingConfig config = null)
        {
            Initialize();

            var rs = new RoutingResponse
            {
                WayPoints = coordinates
            };

            logger.Debug($"Initiating search with {coordinates} waypoints");

            for (var i = 1; i < coordinates.Length; i++)
            {
                var fromCoord = coordinates[i - 1];
                var toCoord = coordinates[i];

                rs.WayPointIndices.Add(new WayPointIndex()
                {
                    CoordinateIndex = rs.Route.Count,
                    LinkReferenceIndex = rs.LinkReferences.Count
                });

                var path = _router.Search(fromCoord, toCoord, config);

                if (!path.Success) throw new Exception("Couldn't find a route between these points.");

                logger.Debug($"Found {path.Links.Length} links.");

                foreach (var link in path.Links)
                {
                    rs.LinkReferences.Add(link.Reference);
                    rs.Links.Add(link);
                    rs.Route.AddRange(link.Geometry.Select(p => new PointUtm33(p.X, p.Y, p.Z)));
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

        public static IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<LinkReference> linkReferences)
        {
            Initialize();
            return _router.GetLinksFromReferences(linkReferences);
        }

        public static void InitializeFromConfig(IConfiguration config)
        {
            if (Directory.Exists(@"data\networks\road\2023-01-09"))
            {
                NetworkFile = @"data\networks\road\2023-01-09\network_skeleton.bin";
                //NetworkFile = @"data\networks\road\2023-01-09\network_three_islands.bin";
                SkeletonConfig = new SkeletonConfig() { LinkDataDirectory = @"data\networks\road\2023-01-09\geometries" };
            }
            else if (config != null)
            {
                NetworkFile = config.GetValue<string>("RoadNetworkLocation");
                SkeletonConfig = new SkeletonConfig() { LinkDataDirectory = config.GetValue<string>("RoadNetworkLinkLocation") };
            }

            Initialize();
        }
    }
}