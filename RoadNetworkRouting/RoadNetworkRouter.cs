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
    public class RoadNetworkRouter
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string Version = "2023-05-29";

        private Graph _graph;
        private SkeletonConfig _skeletonConfig;
        private Dictionary<string, RoadLink> _linkReferenceLookup;
        private readonly object _locker = new();
        public Dictionary<int, RoadLink> Links { get; set; } = null;

        private NearbyBoundsCache<RoadLink> _nearbyLinksLookup = null;
        private readonly int _nearbyLinksRadius = 5000;

        public BoundingBox2D SearchBounds { get; private set; }

        /// <summary>
        /// Network graph that is created the first time this property is accessed, then cached. If changing Links or Vertices, it should be reset.
        /// </summary>
        public Graph Graph
        {
            get => _graph ??= CreateGraph();
            set => _graph = value;
        }

        /// <summary>
        /// How many missing node IDs that were fixed (mapped to existing nodes nearer than 1 meter,
        /// or to newly created nodes) when building the network.
        /// </summary>
        public int FixedMissingNodeIdCount { get; set; }

        public Graph CreateGraph()
        {
            return Graph.Create(Links.Values.Select(p => new GraphDataItem()
            {
                Cost = p.Cost,
                EdgeId = p.LinkId,
                ReverseCost = p.ReverseCost,
                SourceVertexId = p.FromNodeId,
                TargetVertexId = p.ToNodeId
            }));
        }

        public static RoadNetworkRouter Build(IEnumerable<RoadLink> links)
        {
            var router = new RoadNetworkRouter()
            {
                Links = links.ToDictionary(k => k.LinkId, v => v)
            };

            router.FixedMissingNodeIdCount = RoadNetworkUtilities.FixMissingNodeIds(router);

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
            Links = new Dictionary<int, RoadLink>();
        }

        public RoadNetworkRouter(Dictionary<int, RoadLink> links)
        {
            Links = links;
            SearchBounds=BoundingBox2D.Empty();

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
        /// <returns></returns>
        public static RoadNetworkRouter BuildFromGeoJson(string file)
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
                    RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<short>("FT_Fart"),
                    SpeedLimitReversed = properties.Value<short>("TF_Fart"),
                    FromRelativeLength = properties.Value<float>("FROM_M"),
                    ToRelativeLength = properties.Value<float>("TO_M"),
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
            }));
        }

        /// <summary>
        /// Reads the road network from a GeoJSON file where each line is a separate GeoJSON object. Because of constraints in the export functionality, files generated using QGis
        /// will always use the WGS84 coordinate system, and must therefore use a function to transform the coordinates to UTM33.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="wgs84ToUtm33"></param>
        /// <returns></returns>
        public static RoadNetworkRouter BuildFromGeoJsonLines(string file, Func<double, double, (double X, double Y)> wgs84ToUtm33)
        {
            return Build(File.ReadLines(file).Select(line =>
            {
                var feature = JObject.Parse(line);
                var properties = feature["properties"];

                var fromNodeId = properties["fromnode"] == null || string.IsNullOrWhiteSpace(properties.Value<string>("fromnode")) ? int.MinValue : properties.Value<int>("fromnode");
                var toNodeId = properties["tonode"] == null || string.IsNullOrWhiteSpace(properties.Value<string>("tonode")) ? int.MinValue : properties.Value<int>("tonode");

                var geometry = feature["geometry"];
                if (geometry == null) throw new Exception("Invalid GeoJSON (feature missing geometry).");

                var geometryType = geometry.Value<string>("type");
                if (geometryType != "LineString") throw new Exception("Invalid GeoJSON (geometry type must be MultiLineString).");

                var coordinates = geometry["coordinates"].ToObject<double[][]>();

                //{ "type": "Feature", "properties": { "OBJECTID": 1, "linkid": "1", "fromnode": "0", "tonode": "1", "formofway": "1", "funcroadclass": "7", "routeid": "1008609", "from_measure": 0.0, "to_measure": 1.0, "roadnumber": "97911", "oneway": "B", "speedfw": "30", "speedbw": "30", "isferry": "0", "isbridge": "0", "istunnel": "0", "maxweight": null, "maxheight": null, "roadid": "{P97911}", "roadclass": "7", "attributes": null, "bruksklasse": null, "bruksklasse_vi": null, "drivetime_fw": 0.040456461793697801, "drivetime_bw": 0.040456461793697801 }, "geometry": { "type": "LineString", "coordinates": [ [ 10.598823, 60.9589035, 175.396 ], [ 10.5988832, 60.9590235, 175.146 ] ] } }
                var data = new RoadLink
                {
                    LinkId = properties.Value<int>("linkid"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    //RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<short>("speedfw"),
                    SpeedLimitReversed = properties.Value<short>("speedbw"),
                    Cost = properties.Value<float>("drivetime_fw"),
                    ReverseCost = properties.Value<float>("drivetime_bw"),
                    FromRelativeLength = properties.Value<float>("from_measure"),
                    ToRelativeLength = properties.Value<float>("to_measure"),
                    Geometry = coordinates.Select(p => new{Utm=wgs84ToUtm33(p[0], p[1]), Z=p[2]}).Select(p => new Point3D(p.Utm.X, p.Utm.Y, p.Z)).ToArray(),
                    RoadClass = properties.Value<int>("roadclass")
                };
                
                data.Direction = RoadLink.DirectionFromString(properties.Value<string>("oneway"));

                if (data.Direction != RoadLinkDirection.BothWays)
                {
                    if (data.Direction == RoadLinkDirection.AlongGeometry) data.ReverseCost = float.MaxValue;
                    else if (data.Direction == RoadLinkDirection.AgainstGeometry) data.Cost = float.MaxValue;
                    else if (data.Direction == RoadLinkDirection.None) data.ReverseCost = data.Cost = float.MaxValue;
                }

                data.Direction = data.Direction;

                return data;
            }));
        }

        /// <summary>
        /// Loads the road network from a road network binary file (created using the SaveTo function).
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <param name="skeletonConfig"></param>
        /// <returns></returns>
        public static RoadNetworkRouter LoadFrom(string file, Action<int, int> progress = null, SkeletonConfig skeletonConfig = null)
        {
            return LoadFrom(File.OpenRead(file), progress, skeletonConfig);
        }

        /// <summary>
        /// Loads the road network from a road network binary file (created using the SaveTo function).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="progress"></param>
        /// <param name="skeletonConfig"></param>
        /// <returns></returns>
        public static RoadNetworkRouter LoadFrom(Stream stream, Action<int, int> progress = null, SkeletonConfig skeletonConfig = null)
        {
            using var reader = new BinaryReader(stream);

            var formatVersion = reader.ReadInt32(); // Currently at 2

            var router = new RoadNetworkRouter();

            var linkCount = reader.ReadInt32();
            router.Links = new Dictionary<int, RoadLink>(linkCount);

            router.SearchBounds = BoundingBox2D.Empty();

            // If this is a skeleton file, only id and costs are included. The rest must be loaded from individual files later.
            if (formatVersion >= 1000)
            {
                if (skeletonConfig == null) throw new MissingConfigException("Must include a SkeletonConfig object with information about the link data files when loading a skeleton file.");
                router._skeletonConfig = skeletonConfig;
                var itemSize = 4 + 2 * 8 + 7 * 4;
                var buffer = new byte[linkCount * itemSize];
                reader.Read(buffer, 0, buffer.Length);
                var pos = 0;

                for (var i = 0; i < linkCount; i++)
                {
                    var link = new RoadLink
                    {
                        LinkId = BitConverter.ToInt32(buffer, pos),
                        Cost = BitConverter.ToDouble(buffer, pos + 4),
                        ReverseCost = BitConverter.ToDouble(buffer, pos + 12),
                        FromNodeId = BitConverter.ToInt32(buffer, pos + 20),
                        ToNodeId = BitConverter.ToInt32(buffer, pos + 24),
                        Bounds = new BoundingBox2D(BitConverter.ToInt32(buffer, pos + 28), BitConverter.ToInt32(buffer, pos + 32), BitConverter.ToInt32(buffer, pos + 36), BitConverter.ToInt32(buffer, pos + 40)),
                        NetworkGroup = BitConverter.ToInt32(buffer, pos + 40)
                    };
                    router._skeletonConfig.SetSequence(link.LinkId);
                    router.SearchBounds.ExtendSelf(link.Bounds);

                    router.Links.Add(link.LinkId, link);

                    progress?.Invoke(i, linkCount);

                    pos += itemSize;
                }
            }
            else
            {
                for (var i = 0; i < linkCount; i++)
                {
                    var link = new RoadLink();
                    link.ReadFrom(reader);
                    router.SearchBounds.ExtendSelf(link.Bounds);
                    router.Links.Add(link.LinkId, link);
                    progress?.Invoke(i, linkCount);
                }
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
                link.NetworkGroup = analysis.VertexIdGroup[link.FromNodeId];
            }
        }

        /// <summary>
        /// Writes the network to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
        public void SaveTo(string file)
        {
            SetNetworkGroups();

            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var writer = new BinaryWriter(File.Create(file));
            writer.Write(2); // Version

            writer.Write(Links.Count);
            foreach (var link in Links.Values)
            {
                link.WriteTo(writer);
            }
        }

        /// <summary>
        /// Writes the network without detailed link data to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="config"></param>
        public void SaveSkeletonTo(string file, SkeletonConfig config)
        {
            SetNetworkGroups();

            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var linkFiles = Links.Values
                .OrderBy(p => p.Bounds.Xmin / 1000) // An attempt to order links by geography to keep nearby links in the same file
                .ThenBy(p => p.Bounds.Ymin)
                .Sublists(config.LinksPerFile)
                .Select((p, i) => (links:p, ix:i))
                .ToArray();

            using (var writer = new BinaryWriter(File.Create(file)))
            {
                writer.Write(1000 + 2); // Version -- the skeleton version starts at 1000.

                writer.Write(Links.Count);

                foreach (var linkFile in linkFiles)
                foreach (var link in linkFile.links)
                {
                    writer.Write(link.LinkId);
                    writer.Write(link.Cost);
                    writer.Write(link.ReverseCost);
                    writer.Write(link.FromNodeId);
                    writer.Write(link.ToNodeId);
                    writer.Write((int)link.Bounds.Xmin);
                    writer.Write((int)link.Bounds.Xmax + 1);
                    writer.Write((int)link.Bounds.Ymin);
                    writer.Write((int)link.Bounds.Ymax + 1);
                    writer.Write(link.NetworkGroup);
                }
            }

            Directory.CreateDirectory(config.LinkDataDirectory);
            linkFiles.AsParallel().ForAll(lf =>
            {
                using var writer = new BinaryWriter(File.Create(Path.Combine(config.LinkDataDirectory, lf.ix + ".bin")));
                writer.Write(2); // Version
                writer.Write(lf.links.Count);
                foreach (var link in lf.links)
                {
                    link.WriteTo(writer);
                }
            });
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
                timer?.Restart();

                // Find the ID of this link
                var id = link.LinkId;

                // Depending on the skeleton config, there might be more than one link per file.
                // If there is, they are always saved in files of size N, the first file having links
                // from 0 to N-1, the next from N to 2*N-1, and so on.
                var file = _skeletonConfig.GetLinkDataFile(id);

                LoadLinksFromFile(file);

                link = Links[link.LinkId];

                if (link.Geometry == null) throw new Exception("Geometry for link " + id + " was not found in the file '" + file + "'.");
                timer?.Time("routing.loadlinks");
            }

            return link;
        }

        private HashSet<string> _loadedFiles = new();

        private void LoadLinksFromFile(string file)
        {
            lock (_loadedFiles)
            {
                if (_loadedFiles.Contains(file))
                {
                    return;
                }

                // Read the links in this file
                using var reader = new BinaryReader(File.OpenRead(file));

                var formatVersion = reader.ReadInt32();
                var linkCount = reader.ReadInt32();

                // Read data about all links in this link data file onto the links already in the Links dictionary.
                for (var i = 0; i < linkCount; i++)
                {
                    // Read the ID of this link
                    var linkIdToRead = reader.ReadInt32();

                    // Overwrite properties on this link with data from the stream
                    Links[linkIdToRead].ReadFrom(reader, true);
                }

                _loadedFiles.Add(file);
            }
        }

        public void CreateNearbyLinkLookup()
        {
            if (_nearbyLinksLookup != null) return;
            _nearbyLinksLookup = NearbyBoundsCache<RoadLink>.FromBounds(Links.Values, p => p.Bounds, _nearbyLinksRadius);
        }

        public RoutingPoint GetNearestLink(Point3D point, RoutingConfig config, int? networkGroup = null)
        {
            RoutingPoint nearest = null;
            var d = (long)config.InitialSearchRadius;
            CreateNearbyLinkLookup();

            while (nearest == null)
            {
                if (d > config.MaxSearchRadius) throw new NoLinksFoundException($"Found no links near the search point [{point.To2DString()}], using search radius from {config.InitialSearchRadius} to {config.MaxSearchRadius}. Is there something wrong with the coordinates, coordinate system specifications, or radiuses?");

                nearest = _nearbyLinksLookup.GetNearbyItems(point, (int)d)
                    .Where(p => (!networkGroup.HasValue || networkGroup.Value == p.NetworkGroup))
                    .Select(p => EnsureLinkDataLoaded(p))
                    .Select(p => new RoutingPoint(point, p, LineTools.FindNearestPoint(p.Geometry, point.X, point.Y)))
                    .MinBy(p => p.Nearest.DistanceFromLine);

                d *= config.SearchRadiusIncrement;
            }

            return nearest;
        }

        private bool OutsideBounds(Point3D point)
        {
            if(SearchBounds == null) return false;
            return !SearchBounds.Contains(point.X, point.Y, 5_000);
        }

        public RoadNetworkRoutingResult Search(Point3D fromPoint, Point3D toPoint, RoutingConfig config = null, TaskTimer timer = null)
        {
            if (OutsideBounds(fromPoint) || OutsideBounds(toPoint)) throw new InvalidRouteException("The given coordinates are outside of the defined road network area. Please check that you are using the correct source coordinate system.");
            if (Equals(fromPoint, toPoint)) throw new InvalidRouteException("The from and to points sent into the routing function are identical (" + fromPoint + "). Is something wrong with the search coordinates?");

            config ??= new RoutingConfig();
            timer ??= new TaskTimer();

            if (config.SearchRadiusIncrement < 1) throw new InvalidDataException("SearchRadiusIncrement must be larger than 1 to avoid an infinite loop.");

            var source = GetNearestLink(fromPoint, config);
            var target = GetNearestLink(toPoint, config);

            //var bounds = BoundingBox2D.FromPoints(new[] { fromPoint, toPoint }).Extend(5000);
            //var relevantLinks = Links.Values.Where(p => bounds.Overlaps(p.Bounds)).ToArray();
            //var relevantNodes = GenerateVertices(relevantLinks, p => EnsureLinkDataLoaded(p)).Select(p => p.Value).ToArray();
            //GeoJsonCollection.From(relevantLinks.Select(p => p.ToGeoJsonFeature())).WriteTo(@"C:\Users\erlendd\Desktop\Søppel\2023-09-28 - Routing-testing\links.geojson");
            //GeoJsonCollection.From(relevantNodes.Select(p => p.ToGeoJsonFeature())).WriteTo(@"C:\Users\erlendd\Desktop\Søppel\2023-09-28 - Routing-testing\nodes.geojson");
            //GeoJsonCollection.From(new []{ fromPoint, toPoint }, 32633).WriteTo(@"C:\Users\erlendd\Desktop\Søppel\2023-09-28 - Routing-testing\search-points.geojson");
            //GeoJsonCollection.From(new[] { source.Nearest.ToPoint(), target.Nearest.ToPoint()}, 32633).WriteTo(@"C:\Users\erlendd\Desktop\Søppel\2023-09-28 - Routing-testing\entry-points.geojson");

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
                    var alternativeSource = GetNearestLink(source.Point, config, target.Link.NetworkGroup);
                    var alternativeTarget = GetNearestLink(target.Point, config, source.Link.NetworkGroup);

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
                    throw new NotImplementedException("Different group handling for " + config.DifferentGroupHandling + " is not implemented.");
                }
            }

            //GeoJsonCollection.From(new[] { source.Nearest.ToPoint(), target.Nearest.ToPoint() }, 32633).WriteTo(@"C:\Users\erlendd\Desktop\Søppel\2023-09-28 - Routing-testing\entry-points-adjusted.geojson");

            timer.Time("routing.entry");

            return Search(source, target, timer);
        }

        public RoadNetworkRoutingResult Search(RoutingPoint fromPoint, RoutingPoint toPoint, RoutingConfig config = null, TaskTimer timer = null)
        {
            if (fromPoint.Link == null || toPoint.Link == null)
                return Search(fromPoint.Point, toPoint.Point, config, timer);
            return Search(fromPoint, toPoint, timer);
        }

        public RoadNetworkRoutingResult Search(RoutingPoint source, RoutingPoint target, TaskTimer timer = null)
        {
            // Build a network graph for searching
            var graph = Graph;

            timer.Time("routing.graph");

            // Create a graph overloader so that we can insert fake nodes at the from and to points.
            // Without this, the search would be from one of the existing vertices in the road network,
            // which could cause too much of the road link to be included in the results.
            // With the overloader and the fake nodes, we can search from the exact point where the route
            // enters and exits the road network.
            var overloader = new GraphOverloader();

            // Create two fake IDs for the two fake nodes. We use the two absolute lowest values that
            // are possible, since we know these are not used (existing IDs are positive integers).
            var sourceId = int.MinValue;
            var targetId = int.MinValue + 1;

            // Calculate the cost factor by dividing the distance along the link from start to source point
            // by the total length of the link. This is an estimation of how large part of the total edge cost
            // that should count for the "fake" edges from the links FromNode/ToNode to the fake overloaded
            // source node.
            var costFactorSource = source.Nearest.Distance / source.Link.Length;
            var costFactorTarget = target.Nearest.Distance / target.Link.Length;

            // Configure the overloader
            sourceId = overloader.AddSourceOverload(sourceId, source.Link.FromNodeId, source.Link.ToNodeId, costFactorSource);

            targetId = overloader.AddTargetOverload(targetId, target.Link.FromNodeId, target.Link.ToNodeId, costFactorTarget);

            if (sourceId == targetId) throw new InvalidRouteException("Source and target ids are identical. Is something wrong with the search coordinates?");

            // Find the best route between the source and target vertices using the road link costs we have built.
            var route = graph.GetShortestPath(sourceId, targetId, overloader);

            if (route.Edges == null) throw new Exception("Unable to find a route between these coordinates.");

            timer.Time("routing.routing");

            // Extract the road links (if there are any)
            var links = route.Edges?.Select(p => EnsureLinkDataLoaded(Links[p], timer)).ToArray() ?? Array.Empty<RoadLink>();

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
            var nodeId = FindFirstNodeId(links, route.Vertices, source.Point, target.Point);

            var distanceAlongFirst = source.Nearest.Distance;
            if (source.Link.LinkId != links[0].LinkId) distanceAlongFirst = 0;
            var distanceAlongLast = target.Nearest.Distance;
            if (target.Link.LinkId != links[^1].LinkId) distanceAlongLast = 0;

            RotateAndCut(links, nodeId, distanceAlongFirst, distanceAlongLast);

            // If the first or last links are entirely cut, remove them from the link lists.
            if (links[0].Geometry.Length == 0 || links[^1].Geometry.Length == 0)
            {
                links = links.Where(p => p.Geometry.Length > 0).ToArray();
                if (!links.Any()) throw new Exception("Unable to find a route between these coordinates.");
            }

            timer.Time("routing.cut");

            return new RoadNetworkRoutingResult(route, links, source, target, timer);
        }

        /// <summary>
        /// Makes sure all links are rotated correctly (so that they fit together in one coherent group, regardless of
        /// original geometry orientation), and cuts the first and last link to fit the from- and to points if necessary.
        /// </summary>
        /// <param name="links"></param>
        /// <param name="nodeId"></param>
        /// <param name="distanceAlongFirst"></param>
        /// <param name="distanceAlongLast"></param>
        protected void RotateAndCut(RoadLink[] links, int nodeId, double distanceAlongFirst, double distanceAlongLast)
        {
            var (cutStart, cutEnd) = (distanceAlongFirst, links[^1].Length - distanceAlongLast);
            for (var i = 0; i < links.Length; i++)
            {
                // Store the links geometry, and a flag for if it has been modified or not.
                var (geometry, modified, swapNodes, originalLength) = (links[i].Geometry, false, false, links[i].Length);

                // If the next link does not start with this nodeId, turn it around
                // (because it presumably ends with this nodeId -- if not, we're out of luck).
                if (links[i].FromNodeId != nodeId)
                {
                    geometry = links[i].Geometry.Reverse().ToArray();
                    swapNodes = true;
                    modified = true;

                    if (i == 0) cutStart = links[i].Length - cutStart;
                    if (i == links.Length - 1) cutEnd = links[i].Length - cutEnd;
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
        protected static int FindFirstNodeId(RoadLink[] links, int[] vertexIds, Point3D fromPoint, Point3D toPoint)
        {
            // The list of traversed vertices will normally be something like -999, 1, 2, 3, -998, since the
            // first and last vertices are overloaded unless the search points are directly on a vertex.
            
            // If the first vertex is positive, like for example 1, 2, 3, we can return it immediately.
            if (vertexIds[0] >= 0) return vertexIds[0];

            // If the second vertex is positive, like for example -999, 1, 2, 3, -999,
            // we need to make sure the first link is rotated correctly, then return its 
            // first node.
            if (vertexIds.Length > 1 && vertexIds[1] >= 0)
            {
                // If the link's ToNode (end node) is the second vertex,
                // the link is rotated correctly. Return its FromNode.
                if (links[0].ToNodeId == vertexIds[1])
                    return links[0].FromNodeId;

                // Otherwise, it's rotated the wrong way, and we return
                // its ToNode.
                return links[0].ToNodeId;
            }

            // If the second vertex is also negative, that means we have a search on a single link,
            // where both nodes are overloaded. In that case, return the node that minimizes the distances
            // between the nodes and the search points.
            var distancesThisWay = fromPoint.ManhattanDistanceTo(links[0].Geometry[0]) + toPoint.ManhattanDistanceTo(links[0].Geometry[^1]);
            var distancesRotated = fromPoint.ManhattanDistanceTo(links[0].Geometry[^1]) + toPoint.ManhattanDistanceTo(links[0].Geometry[0]);
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
    }
}