using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotSpatial.Data;
using DotSpatial.Topology;
using Extensions.IEnumerableExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Routing;

namespace RoadNetworkRouting
{
    public class RoadNetworkRouter
    {
        private Graph _graph;
        private SkeletonConfig _skeletonConfig;
        public Dictionary<int, GdbRoadLinkData> Links { get; set; } = null;
        public Dictionary<int, NetworkNode> Vertices { get; private set; }

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
                Id = p.Reference,
                SourceVertexId = p.FromNodeId,
                TargetVertexId = p.ToNodeId
            }));
        }

        public static RoadNetworkRouter Build(IEnumerable<GdbRoadLinkData> links)
        {
            var router = new RoadNetworkRouter()
            {
                Links = links.ToDictionary(k => k.LinkId, v => v)
            };

            router.FixedMissingNodeIdCount = RoadNetworkUtilities.FixMissingNodeIds(router);

            var vertices = new Dictionary<int, NetworkNode>();
            foreach (var link in router.Links)
            {
                if (vertices.TryGetValue(link.Value.FromNodeId, out var node))
                    node.Edges++;
                else
                {
                    var point = link.Value.Geometry.Points.First();
                    vertices.Add(link.Value.FromNodeId, new NetworkNode(point.X, point.Y, link.Value.FromNodeId));
                }

                if (vertices.TryGetValue(link.Value.ToNodeId, out node))
                    node.Edges++;
                else
                {
                    var point = link.Value.Geometry.Points.Last();
                    vertices.Add(link.Value.ToNodeId, new NetworkNode(point.X, point.Y, link.Value.ToNodeId));
                }
            }

            router.Vertices = vertices;

            return router;
        }

        private RoadNetworkRouter()
        {
            Links = new Dictionary<int, GdbRoadLinkData>();
            Vertices = new Dictionary<int, NetworkNode>();
        }

        public RoadNetworkRouter(Dictionary<int, GdbRoadLinkData> links, Dictionary<int, NetworkNode> vertices)
        {
            Links = links;
            Vertices = vertices;
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

                var data = new GdbRoadLinkData
                {
                    LinkId = properties.Value<int>("OBJECT_ID"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<short>("FT_Fart"),
                    SpeedLimitReversed = properties.Value<short>("TF_Fart"),
                    FromRelativeLength = properties.Value<float>("FROM_M"),
                    ToRelativeLength = properties.Value<float>("TO_M"),
                    Geometry = new PolyLineZ(coordinates.Select(p => new Point3D(p[0], p[1], p[2])), true)
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
                var data = new GdbRoadLinkData
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
                    Geometry = new PolyLineZ(coordinates.Select(p => new{Utm=wgs84ToUtm33(p[0], p[1]), Z=p[2]}).Select(p => new Point3D(p.Utm.X, p.Utm.Y, p.Z)), true),
                    RoadClass = properties.Value<int>("roadclass")
                };
                
                data.Direction = GdbRoadLinkData.DirectionFromString(properties.Value<string>("oneway"));

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
            using var reader = new BinaryReader(File.OpenRead(file));

            var formatVersion = reader.ReadInt32(); // Currently at 2

            var router = new RoadNetworkRouter();

            var vertexCount = reader.ReadInt32();

            var itemSize = 4 + 8 + 8 + 4;
            var buffer = new byte[vertexCount * itemSize];
            reader.Read(buffer, 0, buffer.Length);

            var pos = 0;
            for (var i = 0; i < vertexCount; i++)
            {
                var id = BitConverter.ToInt32(buffer, pos);
                router.Vertices.Add(id, new NetworkNode(BitConverter.ToDouble(buffer, pos + 4), BitConverter.ToDouble(buffer, pos + 12), id) { Edges = BitConverter.ToInt32(buffer, pos + 20) });
                pos += itemSize;
            }

            var linkCount = reader.ReadInt32();
            router.Links = new Dictionary<int, GdbRoadLinkData>(linkCount);

            // If this is a skeleton file, only id and costs are included. The rest must be loaded from individual files later.
            if (formatVersion >= 1000)
            {
                if (skeletonConfig == null) throw new MissingConfigException("Must include a SkeletonConfig object with information about the link data files when loading a skeleton file.");
                router._skeletonConfig = skeletonConfig;
                itemSize = 4 + 2 * 8 + 6 * 4;
                buffer = new byte[linkCount * itemSize];
                reader.Read(buffer, 0, buffer.Length);
                pos = 0;
                for (var i = 0; i < linkCount; i++)
                {
                    var link = new GdbRoadLinkData
                    {
                        LinkId = BitConverter.ToInt32(buffer, pos),
                        Cost = BitConverter.ToDouble(buffer, pos + 4),
                        ReverseCost = BitConverter.ToDouble(buffer, pos + 12),
                        FromNodeId = BitConverter.ToInt32(buffer, pos + 20),
                        ToNodeId = BitConverter.ToInt32(buffer, pos + 24),
                        Bounds = new BoundingBox2D(BitConverter.ToInt32(buffer, pos + 28), BitConverter.ToInt32(buffer, pos + 32), BitConverter.ToInt32(buffer, pos + 36), BitConverter.ToInt32(buffer, pos + 40))
                    };
                    router._skeletonConfig.SetSequence(link.LinkId);

                    router.Links.Add(link.LinkId, link);

                    progress?.Invoke(i, linkCount);

                    pos += itemSize;
                }
            }
            else
            {
                for (var i = 0; i < linkCount; i++)
                {
                    var link = new GdbRoadLinkData();
                    link.ReadFrom(reader);
                    router.Links.Add(link.LinkId, link);
                    progress?.Invoke(i, linkCount);
                }
            }

            return router;
        }

        /// <summary>
        /// Writes the network to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
        public void SaveTo(string file)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var writer = new BinaryWriter(File.Create(file));
            writer.Write(2); // Version

            WriteVertices(writer);

            writer.Write(Links.Count);
            foreach (var link in Links.Values)
            {
                link.WriteTo(writer);
            }
        }

        private void WriteVertices(BinaryWriter writer)
        {
            writer.Write(Vertices.Count);
            foreach (var v in Vertices.Values)
            {
                writer.Write(v.Id);
                writer.Write(v.X);
                writer.Write(v.Y);
                writer.Write(v.Edges);
            }
        }

        /// <summary>
        /// Writes the network without detailed link data to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="config"></param>
        public void SaveSkeletonTo(string file, SkeletonConfig config)
        {
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

                WriteVertices(writer);

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
        /// <returns></returns>
        public GdbRoadLinkData EnsureLinkDataLoaded(GdbRoadLinkData link)
        {
            if (link.Geometry == null)
                LoadLinkData(link);
            return link;
        }

        /// <summary>
        /// Loads link data for the given link, plus all other links in the same link data file (if any).
        /// </summary>
        /// <param name="link"></param>
        public void LoadLinkData(GdbRoadLinkData link)
        {
            // Find the ID of this link
            var id = link.LinkId;

            // Depending on the skeleton config, there might be more than one link per file.
            // If there is, they are always saved in files of size N, the first file having links
            // from 0 to N-1, the next from N to 2*N-1, and so on.
            var file = _skeletonConfig.GetLinkDataFile(id);

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
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        public (int vertex, double distance) GetNearestVertex(double x, double y)
        {
            if (Vertices?.Any() != true) throw new NullReferenceException("No vertices. Have you remembered to load a road network?");
            var nearby = Vertices.Values.Where(p => Math.Abs(p.Y - y) < 500 && Math.Abs(p.X - x) < 500).ToArray();
            if (nearby.Any())
                return GetNearestVertex(nearby, x, y);

            return GetNearestVertex(Vertices.Values, x, y);
        }

        private (int vertex, double distance) GetNearestVertexDirectly(IEnumerable<NetworkNode> vertices, double x, double y)
        {
            var n = vertices.MinBy(p => Distance(p.X, p.Y, x, y));
            return (n.Id, Distance(n.X, n.Y, x, y));
        }

        public (int vertex, double distance) GetNearestVertex(IEnumerable<NetworkNode> vertices, double x, double y)
        {
            if (vertices?.Any() != true) throw new NullReferenceException("No vertices. Have you remembered to load a road network?");
            var nearby = vertices.Where(p => Math.Abs(p.Y - y) < 500 && Math.Abs(p.X - x) < 500).ToArray();
            if (nearby.Any())
                return GetNearestVertexDirectly(nearby, x, y);

            return GetNearestVertexDirectly(vertices, x, y);
        }

        public (int vertex, double distance) GetNearestVertex(int vertexGroup, double x, double y)
        {
            if (Vertices?.Any() != true) throw new NullReferenceException("No vertices. Have you remembered to load a road network?");
            var nearest = Vertices.Values
                .Where(p => p.VertexGroup == vertexGroup)
                .ToArray();
            return GetNearestVertex(nearest, x, y);
        }

        public NetworkNode GetVertex(int vertexId)
        {
            return Vertices[vertexId];
        }

        public IEnumerable<GdbRoadLinkData> GetLinkReferences(IEnumerable<int> res)
        {
            foreach (var id in res)
                yield return Links[id];
        }

        public void SetVertexGroups(GraphAnalysis analysis)
        {
            foreach (var v in Vertices.Values)
                if (analysis.VertexIdGroup.TryGetValue(v.Id, out var gid))
                    v.VertexGroup = gid;
                else
                    v.VertexGroup = -1;
        }

        public (GdbRoadLinkData Link, NearestPointInfo Nearest) GetNearestVertexFromNearestEdge(Point3D point, RoutingConfig config)
        {
            (GdbRoadLinkData Link, NearestPointInfo Nearest) nearest = (null, null);
            var d = config.InitialSearchRadius;
            while (nearest.Link == null)
            {
                if (d > config.MaxSearchRadius) throw new NoLinksFoundException("Found no links near the search point " + point);

                nearest = Links
                    .Values
                    .Where(p => p.Bounds.Contains(point.X, point.Y, d))
                    .Select(EnsureLinkDataLoaded)
                    .Select(p => (Link: p, Nearest: LineTools.FindNearestPoint(p.Geometry.Points, point.X, point.Y)))
                    .MinBy(p => p.Nearest.DistanceFromLine);

                d *= config.SearchRadiusIncrement;
            }

            return nearest;
        }

        public RoadNetworkRoutingResult Search(Point3D fromPoint, Point3D toPoint, RoutingConfig config = null)
        {
            config ??= new RoutingConfig();

            if (config.SearchRadiusIncrement < 1) throw new InvalidDataException("SearchRadiusIncrement must be larger than 1 to avoid an infinite loop.");

            var source = GetNearestVertexFromNearestEdge(fromPoint, config);
            var target = GetNearestVertexFromNearestEdge(toPoint, config);

            // Build a network graph for searching
            var graph = Graph;

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
            var costFactorSource = source.Nearest.Distance / source.Link.Geometry.Length;
            var costFactorTarget = target.Nearest.Distance / target.Link.Geometry.Length;

            // Configure the overloader
            overloader.AddSourceOverload(sourceId, source.Link.FromNodeId, source.Link.ToNodeId, costFactorSource);
            overloader.AddTargetOverload(targetId, target.Link.FromNodeId, target.Link.ToNodeId, costFactorTarget);

            // Find the best route between the source and target vertices using the road link costs we have built.
            var route = graph.GetShortestPath(sourceId, targetId, overloader);
            
            // Extract the road links (if there are any)
            var links = route.Items?.Select(p => Links[p]).ToArray() ?? Array.Empty<GdbRoadLinkData>();

            // Ensure that link geometries are loaded if this was a skeleton file.
            if (_skeletonConfig != null)
            {
                foreach (var link in links)
                {
                    if (link.Geometry == null)
                    {
                        LoadLinkData(link);
                    }
                }
            }

            // Chop the first and last links so that they start and stop at the search points.
            if (links.Any())
            {
                try
                {
                    links[0] = CutLink(links[0], links[1], source.Nearest.Distance);
                    links[^1] = CutLink(links[^1], links[^2], target.Nearest.Distance);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return new RoadNetworkRoutingResult(route, links);
        }

        private GdbRoadLinkData CutLink(GdbRoadLinkData current, GdbRoadLinkData connectedTo,  double atDistance)
        {
            var connectedAtEnd = current.ToNodeId == connectedTo.FromNodeId || current.ToNodeId == connectedTo.ToNodeId;

            Point3D[] points;
            if(connectedAtEnd)
                points = LineTools.CutStart(current.Geometry.Points, atDistance);
            else
                points = LineTools.CutEnd(current.Geometry.Points, current.Geometry.Length - atDistance);

            // If the cut failed, simply return the entire link.
            // This is a workaround for now, should figure out what makes it fail later.
            if (points.Length < 2)
                return current;

            return current.Clone(new PolyLineZ(points, false));
        }
    }
}