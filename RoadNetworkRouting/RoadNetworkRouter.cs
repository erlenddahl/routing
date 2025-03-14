﻿using System;
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
using System.Globalization;
using EnergyModule.Exceptions;
using Extensions.Utilities;
using Extensions.Utilities.Caching;
using RoadNetworkRouting;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;
using System.Security.Cryptography;
using Extensions.DoubleExtensions;

namespace RoadNetworkRouting
{
    public class RoadNetworkRouter
    {
        public static string Version = "2023-05-29";

        private Graph<RoadLink> _graph;
        private Dictionary<string, RoadLink> _linkReferenceLookup;
        private readonly object _locker = new();
        public Dictionary<int, RoadLink> Links { get; set; } = null;

        private NearbyBoundsCache<RoadLink> _nearbyLinksLookup = null;
        private readonly int _nearbyLinksRadius = 5000;

        public BoundingBox2D SearchBounds { get; private set; }

        public ILinkDataLoader Loader { get; set; }

        /// <summary>
        /// Network graph that is created the first time this property is accessed, then cached. If changing Links or Vertices, it should be reset.
        /// </summary>
        public Graph<RoadLink> Graph
        {
            get => _graph ??= CreateGraph();
            set => _graph = value;
        }

        /// <summary>
        /// How many missing node IDs that were fixed (mapped to existing nodes nearer than 1 meter,
        /// or to newly created nodes) when building the network.
        /// </summary>
        public FixMissingNodeResult FixedMissingNodeIds { get; set; }

        public SplitLinksResult SplitLinksAtIntersections { get; set; }

        public Graph<RoadLink> CreateGraph()
        {
            var graph = new Graph<RoadLink>();
            foreach (var link in Links.Values)
            {
                graph.CreateEdge(link, link.FromNodeId, link.ToNodeId, link.Cost, link.ReverseCost);
            }

            _vertices ??= GenerateVertices();

            return graph;
        }

        public static RoadNetworkRouter Build(IEnumerable<RoadLink> links, NetworkBuildConfig config = null)
        {
            config ??= new NetworkBuildConfig();
            var router = new RoadNetworkRouter(links);

            if (config.PerformLinkSplit)
            {
                router.SplitLinksAtIntersections = RoadNetworkUtilities.SplitLinksAtIntersections(router, config);
            }

            router.FixedMissingNodeIds = RoadNetworkUtilities.FixMissingNodeIds(router, config);

            router._graph = null;

            return router;
        }

        public Dictionary<int, Node> GenerateVertices()
        {
            return GenerateVertices(Links.Values, p => EnsureLinkDataLoaded(p));
        }

        private static Dictionary<int, Node> GenerateVertices(IEnumerable<RoadLink> links, Func<RoadLink, RoadLink> ensureLinkDataLoaded)
        {
            var vertices = new Dictionary<int, Node>();
            foreach (var link in links)
            {
                ensureLinkDataLoaded(link);
                if (vertices.TryGetValue(link.FromNodeId, out var node))
                    node.Edges++;
                else
                {
                    var point = link.Geometry.First();
                    vertices.Add(link.FromNodeId, new Node(point.X, point.Y, link.FromNodeId));
                }

                if (vertices.TryGetValue(link.ToNodeId, out node))
                    node.Edges++;
                else
                {
                    var point = link.Geometry.Last();
                    vertices.Add(link.ToNodeId, new Node(point.X, point.Y, link.ToNodeId));
                }
            }

            return vertices;
        }

        private RoadNetworkRouter()
        {
        }

        public RoadNetworkRouter(Dictionary<int, RoadLink> links)
        {
            Links = links;
            SearchBounds = BoundingBox2D.Empty();

            foreach (var link in Links)
            {
                if (link.Value.Bounds == null)
                    link.Value.Bounds = BoundingBox2D.FromPoints(link.Value.Geometry);
                SearchBounds.ExtendSelf(link.Value.Bounds);
            }
        }

        public RoadNetworkRouter(IEnumerable<RoadLink> links) : this(links.ToDictionary(k => k.LinkId, v => v))
        {
        }

        /// <summary>
        /// Reads the road network from a regular GeoJSON file. Due to memory constraints, this is not usable for large networks.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static RoadNetworkRouter BuildFromGeoJson(string file, NetworkBuildConfig config = null)
        {
            var json = JObject.Parse(File.ReadAllText(file));
            var features = json["features"] as JArray;
            if (features == null) throw new Exception("Invalid GeoJSON (missing features).");

            return Build(features.Select(feature =>
            {
                var properties = feature["properties"];

                var fromNodeId = properties["FromNodeID"] == null || string.IsNullOrWhiteSpace(properties.Value<string>("FromNodeID")) ? int.MinValue : properties.Value<int>("FromNodeID");
                var toNodeId = properties["ToNodeID"] == null || string.IsNullOrWhiteSpace(properties.Value<string>("ToNodeID")) ? int.MinValue : properties.Value<int>("ToNodeID");

                var geometry = feature["geometry"];
                if (features == null) throw new Exception("Invalid GeoJSON (feature missing properties).");
                if (geometry == null) throw new Exception("Invalid GeoJSON (feature missing geometry).");

                var geometryType = geometry.Value<string>("type");
                if (geometryType != "MultiLineString") throw new Exception("Invalid GeoJSON (geometry type must be MultiLineString).");

                var coordinates = geometry["coordinates"][0].ToObject<double[][]>();

                var data = new RoadLink
                {
                    LinkId = properties.Value<int>("OBJECT_ID"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    SpeedLimitKmH = (byte)Math.Max(0, properties.Value<int>("FT_Fart")),
                    SpeedLimitKmHReversed = (byte)Math.Max(0, properties.Value<int>("TF_Fart")),
                    FromRelativeLength = properties.Value<double>("FROM_M"),
                    ToRelativeLength = properties.Value<double>("TO_M"),
                    Geometry = coordinates.Select(p => new Point3D(p[0], p[1], p[2])).ToArray()
                };

                var direction = properties.Value<string>("ONEWAY");
                if (direction != "B")
                {
                    if (direction == "FT") data.ReverseCost = float.MaxValue;
                    else if (direction == "TF") data.Cost = float.MaxValue;
                    else if (direction == "N") data.ReverseCost = data.Cost = float.MaxValue;
                }

                return data;
            }), config);
        }

        /// <summary>
        /// Reads the road network from a GeoJSON file where each line is a separate GeoJSON object. Because of constraints in the export functionality, files generated using QGis
        /// will always use the WGS84 coordinate system, and must therefore use a function to transform the coordinates to UTM33.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="wgs84ToUtm33"></param>
        /// <param name="extractor"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static RoadNetworkRouter BuildFromGeoJsonLines(string file, Func<double, double, (double X, double Y)> wgs84ToUtm33, GeoJsonValueExtractor extractor = null, NetworkBuildConfig config = null)
        {
            config ??= new NetworkBuildConfig();
            extractor ??= new NvdbRoutingNetworkExtractor();

            config.StateReporter?.Invoke("BuildFromGeoJsonLines: " + file);
            config.StateReporter?.Invoke("    > Counting lines ...");
            var lineCount = File.ReadLines(file).Count();
            var processed = 0;

            config.StateReporter?.Invoke("    > Reading GeoJson...");

            return Build(File.ReadLines(file).Select(line =>
            {
                try
                {
                    var feature = JObject.Parse(line);
                    var properties = feature["properties"];
                    if (properties == null) throw new Exception("Invalid GeoJSON (feature missing geometry).");

                    if (extractor.IgnoreLink(properties)) return null;

                    var geometry = feature["geometry"];
                    if (geometry == null) throw new Exception("Invalid GeoJSON (feature missing geometry).");

                    var geometryType = geometry.Value<string>("type");
                    if (geometryType != "LineString") throw new Exception("Invalid GeoJSON (geometry type must be MultiLineString).");

                    var fromNodeId = extractor.GetFromNodeId(properties);
                    var toNodeId = extractor.GetToNodeId(properties);

                    var coordinateContainer = geometry["coordinates"];
                    if (coordinateContainer == null) throw new Exception("Invalid GeoJSON (feature missing geometry.coordinates).");
                    var coordinates = coordinateContainer.ToObject<double[][]>();

                    var data = new RoadLink
                    {
                        LinkId = extractor.GetLinkId(properties),
                        FromNodeId = fromNodeId,
                        ToNodeId = toNodeId,
                        //RoadType = properties.Value<string>("VEGTYPE"),
                        SpeedLimitKmH = extractor.GetSpeedLimitForward(properties),
                        SpeedLimitKmHReversed = extractor.GetSpeedLimitBackwards(properties),
                        Cost = extractor.GetCostForward(properties),
                        ReverseCost = extractor.GetCostBackwards(properties),
                        FromRelativeLength = extractor.GetFromRelativeLength(properties),
                        ToRelativeLength = extractor.GetToRelativeLength(properties),
                        Geometry = new PolyLineZ(coordinates.Select(p => new { Utm = wgs84ToUtm33(p[0], p[1]), Z = p[2] }).Select(p => new Point3D(p.Utm.X, p.Utm.Y, p.Z)), true).Points,
                        RoadClass = extractor.GetRoadClass(properties),
                        LaneCode = extractor.GetLaneCode(properties),
                        RoadWidth = extractor.GetRoadWidth(properties),
                        IsFerry = extractor.IsFerry(properties),
                        IsRoundabout = extractor.IsRoundabout(properties),
                        IsBridge = extractor.IsBridge(properties),
                        IsTunnel = extractor.IsTunnel(properties)
                    };

                    data.Direction = extractor.GetDirection(properties);

                    if (data.Direction != RoadLinkDirection.BothWays)
                    {
                        if (data.Direction == RoadLinkDirection.AlongGeometry) data.ReverseCost = float.MaxValue;
                        else if (data.Direction == RoadLinkDirection.AgainstGeometry) data.Cost = float.MaxValue;
                        else if (data.Direction == RoadLinkDirection.None) data.ReverseCost = data.Cost = float.MaxValue;
                    }

                    data.Direction = data.Direction;

                    config?.ProgressReporter?.Invoke((++processed) / (double)lineCount);

                    return data;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to parse line '" + line + "': " + ex.Message, ex);
                }
            }).Where(p => p != null), config);
        }

        /// <summary>
        /// Loads the road network from a road network binary file (created using the SaveTo function).
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static RoadNetworkRouter LoadFrom(string file, Action<int, int> progress = null)
        {
            return LoadFrom(File.OpenRead(file), progress);
        }

        /// <summary>
        /// Loads the road network from a road network binary file (created using the SaveTo function).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static RoadNetworkRouter LoadFrom(Stream stream, Action<int, int> progress = null)
        {
            using var reader = new BinaryReader(stream);

            var formatVersion = reader.ReadInt32(); // Currently at 4

            var router = new RoadNetworkRouter();

            router.SearchBounds = new BoundingBox2D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            var maxPointCount = reader.ReadInt32();
            _binaryStrings = ReadLookup(reader);

            var linkCount = reader.ReadInt32();
            router.Links = new Dictionary<int, RoadLink>(linkCount);

            var buffer = new byte[RoadLink.CalculateItemSize(maxPointCount, formatVersion)];

            router._graph = new Graph<RoadLink>();
            router._vertices = new Dictionary<int, Node>();
            for (var i = 0; i < linkCount; i++)
            {
                var link = new RoadLink();
                link.ReadFrom(reader, _binaryStrings, buffer, formatVersion);
                router.Links.Add(link.LinkId, link);
                progress?.Invoke(i, linkCount);
                router._graph.CreateEdge(link, link.FromNodeId, link.ToNodeId, link.Cost, link.ReverseCost);

                if (!router._vertices.ContainsKey(link.FromNodeId))
                    router._vertices.Add(link.FromNodeId, new Node(link.Geometry[0].X, link.Geometry[0].Y, link.FromNodeId));
                if (!router._vertices.ContainsKey(link.ToNodeId))
                    router._vertices.Add(link.ToNodeId, new Node(link.Geometry[^1].X, link.Geometry[^1].Y, link.ToNodeId));
            }

            return router;
        }

        /// <summary>
        /// Analyzes the network and sets each link's NetworkGroup according to which sub graph (if any) it belongs to.
        /// Unless it's forced, this function will exit if the first link already has a network group assigned.
        /// </summary>
        public void SetNetworkGroups(bool forced = false)
        {
            var first = Links.Values.First();
            EnsureLinkDataLoaded(first);
            if (!forced && first.NetworkGroup >= 0) return;

            var analysis = Graph.Analyze();
            foreach (var link in Links.Values)
            {
                EnsureLinkDataLoaded(link);

                if (!analysis.VertexIdGroup.TryGetValue(link.FromNodeId, out var g))
                    throw new Exception("Failed to locate group number for node Id " + link.FromNodeId);

                link.NetworkGroup = g;
            }
        }

        /// <summary>
        /// Writes the network to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="writePoints">If set to false, the geometry will not be written. This results in a smaller file, but geometries must be loaded during searches.</param>
        public void SaveTo(string file, bool writePoints = true)
        {
            SetNetworkGroups();

            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var writer = new BinaryWriter(File.Create(file));

            var strings = WriteHeader(writer, 4);

            writer.Write(Links.Count);
            foreach (var link in Links.Values)
            {
                link.WriteTo(writer, strings, writePoints);
            }
        }

        private Dictionary<string, int> WriteHeader(BinaryWriter writer, int version)
        {
            writer.Write(version);

            writer.Write(SearchBounds.Xmin);
            writer.Write(SearchBounds.Xmax);
            writer.Write(SearchBounds.Ymin);
            writer.Write(SearchBounds.Ymax);
            writer.Write(Links.Max(p => p.Value.Geometry.Length));

            var strings = new HashSet<string>();
            foreach (var link in Links.Values)
            {
                strings.Add(link.LaneCode ?? "");
            }

            var stringLookup = ToLookup(strings);

            WriteLookup(writer, stringLookup);

            return stringLookup.ToDictionary(k => k.Value, v => v.Key);
        }

        private Dictionary<int, string> ToLookup(HashSet<string> data)
        {
            return data.Select((p, i) => (p, i)).ToDictionary(k => k.i, v => v.p);
        }

        private void WriteLookup(BinaryWriter writer, Dictionary<int, string> data)
        {
            writer.Write(data.Count);
            foreach (var d in data.OrderBy(p => p.Key)) writer.Write(d.Value);
        }

        private static Dictionary<int, string> ReadLookup(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var lookup = new Dictionary<int, string>(count);
            for (var i = 0; i < count; i++)
            {
                lookup.Add(i, reader.ReadString());
            }

            return lookup;
        }

        /// <summary>
        /// Loads link data for the given link if required, then returns the link (for easy usage in LINQ queries).
        /// </summary>
        /// <param name="link"></param>
        /// <param name="timer"></param>
        /// <returns></returns>
        public RoadLink EnsureLinkDataLoaded(RoadLink link, TaskTimer timer = null)
        {
            if (link.Geometry == null)
            {
                if (Loader == null) throw new Exception("Links are missing data, but no link data loader is defined.");
                timer?.Restart();

                link = Loader.Load(this, link);

                if (link.Geometry == null) throw new Exception("Geometry for link " + link.LinkId + " could not be loaded.");
                timer?.Time("routing.loadlinks");
            }

            return link;
        }

        private static Dictionary<int, string> _binaryStrings;

        private readonly object _nearbyLock = new();
        private Dictionary<int, Node> _vertices;

        public void CreateNearbyLinkLookup()
        {
            if (_nearbyLinksLookup != null) return;
            lock (_nearbyLock)
            {
                if (_nearbyLinksLookup != null) return;
                _nearbyLinksLookup = NearbyBoundsCache<RoadLink>.FromBounds(Links.Values, p => p.Bounds, _nearbyLinksRadius);
            }
        }

        public RoutingPoint GetNearestLink(Point3D point, RoutingConfig config, int? networkGroup = null, int? overrideMaxSearchRadius = null, TaskTimer timer = null)
        {
            RoutingPoint nearest = null;
            var d = (long)config.InitialSearchRadius;
            var maxRadius = overrideMaxSearchRadius ?? config.MaxSearchRadius;
            CreateNearbyLinkLookup();

            while (nearest == null)
            {
                if (d > maxRadius)
                {
                    timer?.Time("routing.entry.failed");
                    throw new NoLinksFoundException($"Found no links{(networkGroup.HasValue?" in the network group " + networkGroup.Value : "")} near the search point [{point.To2DString()}], using search radius from {config.InitialSearchRadius} to {maxRadius}. Is there something wrong with the coordinates, coordinate system specifications, or radiuses?");
                }

                var nearbyLinks = _nearbyLinksLookup.GetNearbyItems(point, (int)d)
                    .Where(p => (!networkGroup.HasValue || networkGroup.Value == p.NetworkGroup))
                    .Select(p => EnsureLinkDataLoaded(p)).ToArray();

                //nearbyLinks = Links.Values.Where(p => p.Bounds.Extend(d).Contains(point)).ToArray();

                var minDistance = double.MaxValue;
                foreach (var link in nearbyLinks)
                {
                    var nearestPoint = LineTools.FindNearestPoint(link.Geometry, point.X, point.Y);
                    if (nearestPoint.DistanceFromLine > minDistance) continue;

                    minDistance = nearestPoint.DistanceFromLine;
                    nearest = new RoutingPoint(point, link, nearestPoint);
                }

                d *= config.SearchRadiusIncrement;
            }

            timer?.Time("routing.entry");

            return nearest;
        }

        private bool OutsideBounds(Point3D point, RoutingConfig config)
        {
            if(SearchBounds == null) return false;
            return !SearchBounds.Contains(point.X, point.Y, config.MaxSearchRadius);
        }

        /// <summary>
        /// Saves debug information as GeoJSON files.
        /// 1. Saves relevant links, nodes and the search points themselves.
        /// 2. Performs a network lookup to find entry and exit points.
        ///     - If this fails, throws the usual exception.
        /// 3. Saves found route.
        /// </summary>
        /// <param name="fromPoint"></param>
        /// <param name="toPoint"></param>
        /// <param name="basePath"></param>
        /// <param name="config"></param>
        public RoadNetworkRoutingResult SaveSearchDebugAsGeoJson(Point3D fromPoint, Point3D toPoint, string basePath, RoutingConfig config, TaskTimer timer)
        {
            var bounds = BoundingBox2D.FromPoints(new[] { fromPoint, toPoint }).Extend(5000);
            var relevantLinks = Links.Values.Where(p => bounds.Overlaps(p.Bounds)).ToArray();
            var relevantNodes = GenerateVertices(relevantLinks, p => EnsureLinkDataLoaded(p)).Select(p => p.Value).ToArray();
           
            GeoJsonCollection.From(relevantLinks.Select(p => p.ToGeoJsonFeature())).WriteTo(basePath + "_relevant-links.geojson");
            GeoJsonCollection.From(relevantNodes.Select(p => p.ToGeoJsonFeature())).WriteTo(basePath + "_relevant-nodes.geojson");
            GeoJsonCollection.From(new []
            {
                GeoJsonFeature.Point(fromPoint.X, fromPoint.Y, 32633, new
                {
                    type="source"
                }),
                GeoJsonFeature.Point(toPoint.X, toPoint.Y, 32633, new
                {
                    type="target"
                })
            }).WriteTo(basePath + "_search-points.geojson");

            var source = GetNearestLink(fromPoint, config, timer: timer);
            var target = GetNearestLink(toPoint, config, timer: timer);

            (source, target) = EnsureEntryPointsAreInTheSameGroup(source, target, config, timer);

            GeoJsonCollection.From(new[]
            {
                GeoJsonFeature.Point(source.Nearest.X, source.Nearest.Y, 32633, new
                {
                    type="source"
                }),
                GeoJsonFeature.Point(target.Nearest.X, target.Nearest.Y, 32633, new
                {
                    type="target"
                })
            }).WriteTo(basePath + "_entry-points.geojson");

            var route = Search(source, target, config, timer, basePath + "_routed_path.geojson");

            GeoJsonCollection
                .From(route.Links
                    .Select(p => p.ToGeoJsonFeature()))
                .WriteTo(basePath + "_found-route.geojson");

            return route;
        }

        public RoadNetworkRoutingResult Search(Point3D fromPoint, Point3D toPoint, RoutingConfig config = null, TaskTimer timer = null)
        {
            if (Equals(fromPoint, toPoint))
            {
                throw new IdenticalSearchPointsException("The from and to points sent into the routing function are identical (" + fromPoint + "). Is something wrong with the search coordinates?");
            }
            
            config ??= new RoutingConfig();
            timer ??= new TaskTimer();

            if (OutsideBounds(fromPoint, config) || OutsideBounds(toPoint, config)) throw new RoutingException("The given coordinates are outside of the defined road network area. Please check that you are using the correct source coordinate system, and that you have selected a network that covers the area you're requesting.");

            if (config.SearchRadiusIncrement <= 1) throw new NegativeSearchRadiusIncrementException("SearchRadiusIncrement must be larger than 1 to avoid an infinite loop.");

            var source = GetNearestLink(fromPoint, config, timer: timer);
            var target = GetNearestLink(toPoint, config, timer: timer);

            (source, target) = EnsureEntryPointsAreInTheSameGroup(source, target, config, timer);
            
            timer.Time("routing.entry");

            return Search(source, target, config, timer);
        }

        private (RoutingPoint source, RoutingPoint target) EnsureEntryPointsAreInTheSameGroup(RoutingPoint source, RoutingPoint target, RoutingConfig config, TaskTimer timer)
        {
            // If the source and target entry points are in different disconnected parts of the road network (for example if one of them is on an island),
            // we can't directly find a route between them.
            if (source.Link.NetworkGroup != target.Link.NetworkGroup)
            {
                // If the config is set to only route between nodes/links in the same network group, raise an exception.
                if (config.DifferentGroupHandling == GroupHandling.OnlySame)
                {
                    throw new DifferentGroupsException("The source and target links were in different sub graphs in a disconnected road network, which is not allowed according to the given config.DifferentGroupHandling.");
                }

                // If we are allowed to, we can pick the best group.
                if (config.DifferentGroupHandling == GroupHandling.BestGroup)
                {
                    // Find the alternative entry/exit points in each of the two network groups.
                    var alternativeSource = GetNearestLink(source.SearchPoint, config, target.Link.NetworkGroup, int.MaxValue, timer);
                    var alternativeTarget = GetNearestLink(target.SearchPoint, config, source.Link.NetworkGroup, int.MaxValue, timer);

                    // Update either the source or the target, depending on which gives the smallest distance from the entry/exit points to the source/target coordinates.
                    if (source.Nearest.DistanceFromLine + alternativeTarget.Nearest.DistanceFromLine < alternativeSource.Nearest.DistanceFromLine + target.Nearest.DistanceFromLine)
                    {
                        target = alternativeTarget;
                    }
                    else
                    {
                        source = alternativeSource;
                    }
                }
                else
                {
                    throw new MissingGroupHandlingException("Different group handling for " + config.DifferentGroupHandling + " is not implemented.");
                }
            }

            return (source, target);
        }

        public RoadNetworkRoutingResult Search(RoutingPoint source, RoutingPoint target, RoutingConfig config = null, TaskTimer timer = null, string saveRouteDebugDataTo = null)
        {
            if (source.Link == null || target.Link == null || source.Link.NetworkGroup != target.Link.NetworkGroup)
                return Search(source.SearchPoint, target.SearchPoint, config, timer);

            // Build a network graph for searching
            var graph = Graph;

            config ??= new RoutingConfig();
            timer ??= new TaskTimer();
            timer.Time("routing.graph");

            // Create a graph overloader so that we can insert fake nodes at the from and to points.
            // Without this, the search would be from one of the existing vertices in the road network,
            // which could cause too much of the road link to be included in the results.
            // With the overloader and the fake nodes, we can search from the exact point where the route
            // enters and exits the road network.
            var overloader = new GraphOverloader<RoadLink>();

            // Create two fake IDs for the two fake nodes. We use the two absolute lowest values that
            // are possible, since we know these are not used (existing IDs are positive integers).
            var sourceId = int.MinValue;
            var targetId = int.MinValue + 1;

            EnsureLinkDataLoaded(source.Link);
            EnsureLinkDataLoaded(target.Link);

            // Calculate the cost factor by dividing the distance along the link from start to source point
            // by the total length of the link. This is an estimation of how large part of the total edge cost
            // that should count for the "fake" edges from the links FromNode/ToNode to the fake overloaded
            // source node.
            var costFactorSource = source.Nearest.Distance / source.Link.LengthM;
            var costFactorTarget = target.Nearest.Distance / target.Link.LengthM;

            // Configure the overloader
            sourceId = overloader.AddSourceOverload(sourceId, source.Link.FromNodeId, source.Link.ToNodeId, costFactorSource);
            targetId = overloader.AddTargetOverload(targetId, target.Link.FromNodeId, target.Link.ToNodeId, costFactorTarget);

            if (sourceId == targetId) throw new IdenticalSourceAndTargetException("Source and target ids in the routing graph are identical. Is something wrong with the search coordinates?");

            // The cost is travel time measured in minutes. Calculate a reasonable maximum amount of time using
            // the Manhattan distance between the search points, and use this as a maximum cost to make Dijkstra avoid
            // unnecessary searching if the target point is unreachable.
            var distanceEstimate = Point3D.ManhattanDistanceTo2D(source.Nearest.X, source.Nearest.Y, target.Nearest.X, target.Nearest.Y);

            // If the nearest points are somewhere within a road link, the search will have to take into account the length of this road link.
            // Make sure the estimate is at least the sum of the two involved links.
            distanceEstimate = Math.Max(distanceEstimate, source.Link.LengthM + target.Link.LengthM);

            // Make the conservative assumption that the manhattan distance could be a fifth of the actual distance
            distanceEstimate *= 3;

            var maxIterations = Math.Max((long)distanceEstimate, 250);

            // For debugging
            var keepDynamicData = saveRouteDebugDataTo != null;

            // Find the best route between the source and target vertices using the road link costs we have built.
            QuickGraphSearchResult<RoadLink> route;
            if (config.Algorithm == RoutingAlgorithm.AStar)
            {
                var b = (target.Nearest.X, target.Nearest.Y);
                route = graph.GetShortestPathAstar(sourceId, targetId, (curr, _) =>
                {
                    if (curr.Id == targetId) return 0;
                    var a = _vertices.TryGetValue(curr.Id, out var va) ? (va.X, va.Y) : (source.Nearest.X, source.Nearest.Y);
                    return Heuristic(a, b);
                }, overloader, maxSearchDurationMs: config.MaxSearchDurationMs, maxIterations: maxIterations, keepDynamicData:keepDynamicData);
            }
            else
            {
                // Assume that the average speed is 3.6 km/h => 1 m/s (very low, to avoid cutting searches too short).
                var maxCost = (distanceEstimate / 1) / 60d;

                // Dijkstra needs a lot more iterations than A* because it searches in all directions.
                maxIterations *= 100;

                route = graph.GetShortestPath(sourceId, targetId, overloader, Math.Max(maxCost, 25), config.MaxSearchDurationMs, maxIterations, keepDynamicData: keepDynamicData);
            }
            timer.Time("routing.routing");

            if (saveRouteDebugDataTo != null)
            {
                SaveDijkstraSearch(saveRouteDebugDataTo, route, source.SearchPoint, target.SearchPoint);
            }

            if (route.Edges == null)
                throw new RoutingException("Unable to find a route between these coordinates (different networks? errors in the network?) [it=" + route.InternalData.Iterations + ", aboveMax=" + route.InternalData.AboveMaxCost + ", term=" + route.InternalData.Termination + "].", route);

            // Extract the road links (if there are any)
            var links = route.Edges?.Select(p => EnsureLinkDataLoaded(p, timer)).ToArray() ?? Array.Empty<RoadLink>();

            // In cases where the routing result is a single link, it will be listed twice because of the
            // GraphOverloader's fake edges. Use only one of them in those cases.
            if (source.Link.LinkId == target.Link.LinkId && links.Length == 2 && links[0].LinkId == links[1].LinkId)
            {
                links = links.Take(1).ToArray();
            }

            // In cases where the routing is along a single one-way link, we may get weird results. This is a hack for that.
            if (source.Link.LinkId == target.Link.LinkId && (source.Link.Direction == RoadLinkDirection.AlongGeometry && target.Nearest.Distance > source.Nearest.Distance || source.Link.Direction == RoadLinkDirection.AgainstGeometry && target.Nearest.Distance < source.Nearest.Distance))
            {
                links = new[] { source.Link };
            }

            timer.Time("routing.post");

            // Chop the first and last links so that they start and stop at the search points,
            // and reverse the geometry of any links that are FT/TF when they should be TF/FT.
            // Store which nodeId the next link should start with.
            var nodeId = FindFirstNodeId(links, route.Vertices, source.SearchPoint, target.SearchPoint);

            var distanceAlongFirst = GetDistanceAlong(source, links[0]);
            var distanceAlongLast = GetDistanceAlong(target, links[^1]);

            var originalLinkCount = links.Length;
            var originalLinkLength = links.Sum(p => p.LengthM);

            RotateAndCut(links, nodeId, distanceAlongFirst, distanceAlongLast);

            // If the first or last links are entirely cut, remove them from the link lists.
            if (links[0].Geometry.Length == 0 || links[^1].Geometry.Length == 0)
            {
                links = links.Where(p => p.Geometry.Length > 0).ToArray();
                if (!links.Any()) throw new EmptyRouteException($"The resulting route after post-processing contains zero road links (too short distance between search points?) [orig={originalLinkCount}, ol={originalLinkLength:n2}, daf={distanceAlongFirst:n2}, dal={distanceAlongLast:n2}].", route);
            }

            timer.Time("routing.cut");

            return new RoadNetworkRoutingResult(route, links, source, target, timer);
        }

        private double GetDistanceAlong(RoutingPoint point, RoadLink link)
        {
            var distanceAlong = point.Nearest.Distance;
            if (point.Link.LinkId != link.LinkId)
            {
                var p = point.Nearest.ToPoint();
                if (p.DistanceTo2D(link.Geometry[0]) < p.DistanceTo2D(link.Geometry[^1]))
                    distanceAlong = 0;
                else
                    distanceAlong = link.LengthM;
            }
            return distanceAlong;
        }

        /// <summary>
        /// Calculates a heuristic between points. Since the cost is based on varying data, this will probably
        /// need to be changed somehow in the future. For now, make it right for the only huge network (NMA/NPRA
        /// road network). This network has a cost that is measured in seconds (driving time).
        /// Therefore, we make a heuristic that is measured in approximate minutes.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Heuristic((double X, double Y) a, (double X, double Y) b)
        {
            // Calculate the distance estimate (Manhattan distance) between the points
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var manhattan = Math.Abs(dx) + Math.Abs(dy);

            // Assume average driving speed of 25 m/s (~90 km/h) to
            // get a relatively realistic estimate of driving time
            // This is intentionally a quite high average, to ensure
            // the heuristic under-estimates the true cost (otherwise
            // we will get sub-optimal solutions).
            var seconds = manhattan / 25d;

            return seconds;
        }

        private void SaveDijkstraSearch(string path, QuickGraphSearchResult<RoadLink> route, Point3D fromPoint, Point3D toPoint)
        {
            var bounds = BoundingBox2D.FromPoints(new[] { fromPoint, toPoint }).Extend(250000);
            var relevantLinks = Links.Values.Where(p => bounds.Overlaps(p.Bounds)).ToArray();
            var relevantNodes = GenerateVertices(relevantLinks, p => EnsureLinkDataLoaded(p));

            //var nodeLookup = GenerateVertices();
            GeoJsonCollection.From(route.InternalData.GetInternalData().Select(p =>
            {
                if (!relevantNodes.TryGetValue(p.Vertex.Id, out var node)) return null;
                var h = Heuristic((node.X, node.Y), (toPoint.X, toPoint.Y));
                var c = double.IsInfinity(p.Cost) ? 999999 : p.Cost;
                return GeoJsonFeature.Point(node.X, node.Y, new
                {
                    Cost = c,
                    p.Vertex.Id,
                    p.Visited,
                    node.Edges,
                    node.VertexGroup,
                    Heuristic = h,
                    SumCostHeu = c + h
                });
            }).Where(p => p != null)).WriteTo(path);
        }

        /// <summary>
        /// Makes sure all links are rotated correctly (so that they fit together in one coherent group, regardless of
        /// original geometry orientation), and cuts the first and last link to fit the from- and to points if necessary.
        /// </summary>
        /// <param name="links"></param>
        /// <param name="nodeId"></param>
        /// <param name="distanceAlongFirst">How far along the first link the start point is. Everything before this will be cut away. For example, if distanceAlongFirst is 5, meters 0-5 of the first link will be cut away.</param>
        /// <param name="distanceAlongLast">How far along the last link the end point is. Everything after this will be cut away. For example, if distanceAlongLast is 5, meters 5-N of the last link will be cut away.</param>
        public static void RotateAndCut(RoadLink[] links, int nodeId, double distanceAlongFirst, double distanceAlongLast)
        {
            var (cutStart, cutEnd) = (distanceAlongFirst, links[^1].LengthM - distanceAlongLast);
            for (var i = 0; i < links.Length; i++)
            {
                // Store the links geometry, and a flag for if it has been modified or not.
                var (geometry, modified, swapNodes, originalLength) = (links[i].Geometry, false, false, Length: links[i].LengthM);

                // If the next link does not start with this nodeId, turn it around
                // (because it presumably ends with this nodeId -- if not, we're out of luck).
                if (links[i].FromNodeId != nodeId)
                {
                    geometry = links[i].Geometry.Reverse().ToArray();
                    swapNodes = true;
                    modified = true;
                    
                    if (i == 0) cutStart = links[i].LengthM - cutStart;
                    if (i == links.Length - 1) cutEnd = links[i].LengthM - cutEnd;
                }

                // If this is the first link, remove points from the start if necessary (if
                // the search point/start point is inside the link).
                if (i == 0 && cutStart > 0)
                {
                    geometry = LineTools.CutStart(geometry, cutStart, 0.0000001);
                    modified = true;
                }

                if (i == links.Length - 1 && cutEnd > 0)
                {
                    geometry = LineTools.CutEnd(geometry, cutEnd, 0.0000001);
                    modified = true;
                }

                // If the geometry was modified, replace this link in the links array with
                // an identical link with the new geometry.
                if (modified)
                {
                    links[i] = links[i].Clone(geometry);

                    // Update the FromRelLen and ToRelLen. Calculate how large part of the link this part represented before.
                    var originalRelativeLength = links[i].ToRelativeLength - links[i].FromRelativeLength;

                    // Calculate how large ratio was cut at the start
                    // For example, if we cut 10 meters of a segment that was 40 meters, and the FromRelativeLength was 0.3 and the
                    // ToRelativeLength was 0.7, that means we had a full link of a 100 meters, resulting in a new FromRelativeLength
                    // that should be 0.3 + 10 / (40 / 0.4) = 0.3 + 10 / 100 = 0.3 + 0.1 = 0.4.
                    if (i == 0 && cutStart > 0)
                        links[i].FromRelativeLength += cutStart / (originalLength / originalRelativeLength);

                    // ... and/or at the end
                    if (i == links.Length - 1 && cutEnd > 0)
                        links[i].ToRelativeLength -= cutEnd / (originalLength / originalRelativeLength);

                    if (swapNodes)
                    {
                        (links[i].FromNodeId, links[i].ToNodeId) = (links[i].ToNodeId, links[i].FromNodeId);
                    }
                }

                // Now save the end node of this link as the node the next link should 
                // start with.
                nodeId = links[i].ToNodeId;
            }
        }

        /// <summary>
        /// Given a list of links, detect what node (from the first link)
        /// that is the first node of the route. 
        /// </summary>
        /// <param name="links"></param>
        /// <param name="vertexIds"></param>
        /// <param name="fromPoint"></param>
        /// <param name="toPoint"></param>
        /// <returns></returns>
        public static int FindFirstNodeId(RoadLink[] links, int[] vertexIds, Point3D fromPoint, Point3D toPoint)
        {
            // The list of traversed vertices will normally be something like -999, 1, 2, 3, -998, since the
            // first and last vertices are overloaded unless the search points are directly on a vertex.
            
            // If the first vertex is positive, like for example 1, 2, 3, we can return it immediately.
            if (vertexIds[0] >= 0) return vertexIds[0];

            // If the second vertex is positive, like for example -999, 1, 2, 3, -999,
            // we need to make sure the first link is rotated correctly, then return its 
            // first node.
            if (vertexIds.Length > 1 && vertexIds[1] >= 0 && (vertexIds.Length < 3 || vertexIds[2] >= 0))
            {
                // If the link's ToNode (end node) is the second vertex,
                // the link is rotated correctly. Return its FromNode.
                if (links[0].ToNodeId == vertexIds[1])
                    return links[0].FromNodeId;

                // Otherwise, it's rotated the wrong way, and we return
                // its ToNode.
                return links[0].ToNodeId;
            }

            // If the second or third vertex is also negative, that means we have a search on a single link,
            // where both nodes are overloaded. In that case, return the node that minimizes the distances
            // between the nodes and the search points.
            var distancesThisWay = fromPoint.DistanceTo2D(links[0].Geometry[0]) + toPoint.DistanceTo2D(links[0].Geometry[^1]);
            var distancesRotated = fromPoint.DistanceTo2D(links[0].Geometry[^1]) + toPoint.DistanceTo2D(links[0].Geometry[0]);
            if(distancesThisWay < distancesRotated) return links[0].FromNodeId;
            return links[0].ToNodeId;
        }

        public IEnumerable<RoadLink> GetLinksFromReferences(IEnumerable<string> linkReferences)
        {
            lock (_locker)
            {
                if (_linkReferenceLookup == null)
                {
                    _linkReferenceLookup = Links
                        .Select(p => EnsureLinkDataLoaded(p.Value))
                        .ToDictionary(k => k.Reference.ToShortRepresentation(), v => v);
                }
            }

            foreach (var lr in linkReferences)
                if (_linkReferenceLookup.TryGetValue(lr, out var link))
                    yield return link;
                else
                    throw new Exception("A link with the reference code '" + lr + "' was not found in the current road network.");
        }

        /// <summary>
        /// Saves a GeoJSON representation of this routing network, including network analysis data.
        /// The data is saved as GeoJSON lines (.geojsonl), where each line in the file is one complete GeoJSON feature.
        /// Coordinates are saved in WGS84 / EPSG:4326 unless otherwise specified.
        /// </summary>
        /// <param name="edgeFile"></param>
        /// <param name="vertexFile"></param>
        /// <param name="srid"></param>
        public void SaveGeoJsonTo(string edgeFile, string vertexFile, int srid = 4326)
        {
            var graph = Graph;
            SetNetworkGroups();

            var vertexLocations = Links.Values
                .SelectMany(p => new[] { (id:p.FromNodeId, location:p.Geometry[0], group:p.NetworkGroup), (id:p.ToNodeId, location:p.Geometry[^1], group:p.NetworkGroup) })
                .GroupBy(p => p.Item1)
                .ToDictionary(k => k.Key, v => v.First());

            var converter = CoordinateConverter.FromUtm33(srid);
            
            GeoJsonCollection.WriteLinesTo(vertexFile, graph.Vertices
                .Select(p => vertexLocations[p.Key])
                .Select(p =>
                    GeoJsonFeature.Point(p.location, converter, new
                    {
                        p.id,
                        p.group
                    })));

            GeoJsonCollection.WriteLinesTo(edgeFile, Links.Values.Select(p => p.ToGeoJsonFeature()));
        }
    }

    public class NvdbRoutingNetworkExtractor : GeoJsonValueExtractor
    {
        //{ "type": "Feature", "properties": { "OBJECTID": 1, "linkid": "1", "fromnode": "0", "tonode": "1", "formofway": "1", "funcroadclass": "7", "routeid": "1008609", "from_measure": 0.0, "to_measure": 1.0, "roadnumber": "97911", "oneway": "B", "speedfw": "30", "speedbw": "30", "isferry": "0", "isbridge": "0", "istunnel": "0", "maxweight": null, "maxheight": null, "roadid": "{P97911}", "roadclass": "7", "attributes": null, "bruksklasse": null, "bruksklasse_vi": null, "drivetime_fw": 0.040456461793697801, "drivetime_bw": 0.040456461793697801 }, "geometry": { "type": "LineString", "coordinates": [ [ 10.598823, 60.9589035, 175.396 ], [ 10.5988832, 60.9590235, 175.146 ] ] } }

        public override int GetFromNodeId(JToken properties) => GetSafeInt(properties, "fromnode");
        public override int GetToNodeId(JToken properties) => GetSafeInt(properties, "tonode");
        public override int GetLinkId(JToken properties) => properties.Value<int>("linkid");
        public override byte GetSpeedLimitForward(JToken properties) => properties.Value<byte>("speedfw");
        public override byte GetSpeedLimitBackwards(JToken properties) => properties.Value<byte>("speedbw");
        public override float GetCostForward(JToken properties) => properties.Value<float>("drivetime_fw");
        public override float GetCostBackwards(JToken properties) => properties.Value<float>("drivetime_bw");
        public override double GetFromRelativeLength(JToken properties) => properties.Value<float>("from_measure");
        public override double GetToRelativeLength(JToken properties) => properties.Value<float>("to_measure");
        public override byte GetRoadClass(JToken properties) => properties.Value<byte>("roadclass");
        public override RoadLinkDirection GetDirection(JToken properties) => RoadLink.DirectionFromString(properties.Value<string>("oneway"));
        public override string GetLaneCode(JToken properties) => null;
        public override float GetRoadWidth(JToken properties) => 8;
        public override bool IsFerry(JToken properties) => properties.Value<byte>("isferry") == 1;
        public override bool IsRoundabout(JToken properties) => false;
        public override bool IsBridge(JToken properties) => properties.Value<byte>("isbridge") == 1;
        public override bool IsTunnel(JToken properties) => properties.Value<byte>("istunnel") == 1;
        public override bool IgnoreLink(JToken properties) => false;
    }

    /// <summary>
    /// Extracts GeoJSON values from the Norwegian Mapping Authority dataset (https://kartkatalog.geonorge.no/metadata/nvdb-rutedatasett/19ee16b4-ebe3-4e9d-8d86-0dc6b31f5c99)
    /// </summary>
    public class NmaRoutingNetworkExtractor : GeoJsonValueExtractor
    {
        public override int GetFromNodeId(JToken properties) => int.MinValue;
        public override int GetToNodeId(JToken properties) => int.MinValue;
        public override int GetLinkId(JToken properties) => properties.Value<int>("veglenkeid");
        public override byte GetSpeedLimitForward(JToken properties) => GetSafeByte(properties, "fartsgrense", 50);
        public override byte GetSpeedLimitBackwards(JToken properties) => GetSafeByte(properties, "fartsgrense", 50);
        public override float GetCostForward(JToken properties) => properties.Value<float>("geometrilengde") / (GetSafeByte(properties, "fartsgrense", 50) / 3.6f); 
        public override float GetCostBackwards(JToken properties) => properties.Value<float>("geometrilengde") / (GetSafeByte(properties, "fartsgrense", 50) / 3.6f);
        public override double GetFromRelativeLength(JToken properties) => properties.Value<float>("fra_posisjon");
        public override double GetToRelativeLength(JToken properties) => properties.Value<float>("til_posisjon");
        public override byte GetRoadClass(JToken properties) => GetSafeByte(properties, "funksjonellvegklasse", 0);
        public override RoadLinkDirection GetDirection(JToken properties) => DirectionFromString(properties.Value<string>("kjoreretning"));
        public override string GetLaneCode(JToken properties) => properties.Value<string>("feltoversikt");
        public override float GetRoadWidth(JToken properties) => properties.Value<string>("feltoversikt").Split('#').Length * 3.5f;
        public override bool IsFerry(JToken properties) => properties.Value<string>("typeveg") == "Bilferje";
        public override bool IsRoundabout(JToken properties) => properties.Value<string>("typeveg").StartsWith("Rundkj");

        public override bool IsBridge(JToken properties) => properties.Value<string>("medium") == "L";
        public override bool IsTunnel(JToken properties) => properties.Value<string>("medium") == "U";
        public override bool IgnoreLink(JToken properties)
        {
            var t = properties.Value<string>("typeveg");
            if (t == "Gangveg") return true;
            if (t == "Gangfelt") return true;
            if (t == "Gågate" || t == "GÃ¥gate") return true;
            if (t == "Gang- og sykkelveg") return true;
            if (t == "Fortau") return true;
            if (t == "Trapp") return true;
            if (t == "Sykkelveg") return true;

            return false;
        }

        private RoadLinkDirection DirectionFromString(string direction)
        {
            switch (direction.ToLower())
            {
                case "begge":
                    return RoadLinkDirection.BothWays;
                case "med":
                    return RoadLinkDirection.AlongGeometry;
                case "mot":
                    return RoadLinkDirection.AgainstGeometry;
                default:
                    throw new Exception("Ukjent kjøreretning: '" + direction + "'");
            }

            throw new Exception("Unknown road link direction: '" + direction + "' (must be B, FT, TF, or N).");
        }
    }

    public abstract class GeoJsonValueExtractor
    {
        public abstract int GetFromNodeId(JToken properties);
        public abstract int GetToNodeId(JToken properties);

        protected int GetSafeInt(JToken properties, string key, int defaultValue = int.MinValue)
        {
            return properties[key] == null || string.IsNullOrWhiteSpace(properties.Value<string>(key)) ? defaultValue : properties.Value<int>(key);
        }

        protected byte GetSafeByte(JToken properties, string key, byte defaultValue = byte.MinValue)
        {
            return properties[key] == null || string.IsNullOrWhiteSpace(properties.Value<string>(key)) ? defaultValue : properties.Value<byte>(key);
        }

        public abstract int GetLinkId(JToken properties);
        public abstract byte GetSpeedLimitForward(JToken properties);
        public abstract byte GetSpeedLimitBackwards(JToken properties);
        public abstract float GetCostForward(JToken properties);
        public abstract float GetCostBackwards(JToken properties);
        public abstract double GetFromRelativeLength(JToken properties);
        public abstract double GetToRelativeLength(JToken properties);
        public abstract byte GetRoadClass(JToken properties);
        public abstract RoadLinkDirection GetDirection(JToken properties);
        public abstract string GetLaneCode(JToken properties);
        public abstract float GetRoadWidth(JToken properties);
        public abstract bool IsFerry(JToken properties);
        public abstract bool IsRoundabout(JToken properties);
        public abstract bool IsBridge(JToken properties);
        public abstract bool IsTunnel(JToken properties);
        public abstract bool IgnoreLink(JToken properties);
    }
}