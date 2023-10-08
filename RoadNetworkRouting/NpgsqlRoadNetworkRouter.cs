using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Extensions.IEnumerableExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Routing;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Exceptions;
using RoadNetworkRouting.Network;
using RoadNetworkRouting.Utils;
using System.Diagnostics.Metrics;
using EnergyModule.Network;
using System.Drawing;
using EnergyModule.Exceptions;
using Extensions.Utilities;
using Extensions.Utilities.Caching;
using NLog.Targets;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Service;

namespace RoadNetworkRouting
{
    public class NpgsqlRoadNetworkRouter
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string Version = "2023-05-29";

        private Graph<GraphDataItem> _graph;
        private readonly object _locker = new();

        private NearbyBoundsCache<RoadLink> _nearbyLinksLookup = null;
        private readonly int _nearbyLinksRadius = 5000;

        public BoundingBox2D SearchBounds { get; private set; }

        public ILinkDataLoader Loader { get; set; }

        private NpgsqlRoadNetworkRouter()
        {
        }
    }
}