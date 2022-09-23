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
        public Dictionary<int, GdbRoadLinkData> Links { get; set; } = null;
        public Dictionary<int, NetworkNode> Vertices { get; private set; }

        /// <summary>
        /// How many missing node IDs that were fixed (mapped to existing nodes nearer than 1 meter,
        /// or to newly created nodes) when building the network.
        /// </summary>
        public int FixedMissingNodeIdCount { get; set; }

        public Graph GetGraph()
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

            router.FixedMissingNodeIdCount = router.FixMissingNodeIds();

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

        private struct Node
        {
            public readonly int Id;
            public readonly Point3D Location;

            public Node(Point3D location, int id)
            {
                Location = location;
                Id = id;
            }
        }

        private int FixMissingNodeIds()
        {
            // Create a list containing all nodes with their IDs and locations, then store it as a
            // Y-separated dictionary of lists.
            var nodesByY = Links
                .SelectMany(p => new[]
                {
                    new Node(p.Value.Geometry.Points.First(), p.Value.FromNodeId),
                    new Node(p.Value.Geometry.Points.Last(), p.Value.ToNodeId)
                })
                .Where(p => p.Id > int.MinValue)
                .GroupBy(p => (int)p.Location.Y)
                .ToDictionary(k => k.Key, v => v.ToList());

            // Locate the max node ID, so that we can continue creating nodes with IDs higher than this.
            var id = nodesByY.SelectMany(p => p.Value).Max(p => p.Id) + 1;

            List<Node> FindRelevant(Point3D loc)
            {
                var relevantNodes = new List<Node>();
                if (nodesByY.TryGetValue((int)loc.Y, out var nodes)) relevantNodes.AddRange(nodes);
                if (nodesByY.TryGetValue((int)loc.Y - 1, out var nodesBelow)) relevantNodes.AddRange(nodesBelow);
                if (nodesByY.TryGetValue((int)loc.Y + 1, out var nodesAbove)) relevantNodes.AddRange(nodesAbove);
                return relevantNodes;
            }

            void AddNode(Node node)
            {
                if (!nodesByY.TryGetValue((int)node.Location.Y, out var list))
                    nodesByY.Add((int)node.Location.Y, list = new List<Node>());

                list.Add(node);
            }

            // Keep track of how many we fixed
            var fixedNodes = 0;

            Node FindMatchingNode(Point3D location, string source)
            {
                // Next, retrieve any nodes that could be relevant by doing a simple dictionary lookup.
                var relevantNodes = FindRelevant(location);

                // Finally, find any nodes within 1 meter from this location.
                // (Using ManhattanDistance as a filter first, as the Sqrt calculation in the actual calculation is expensive.)
                var match = relevantNodes.FirstOrDefault(p => p.Location.ManhattanDistanceTo2D(location) < 2 && p.Location.DistanceTo2D(location) <= 1);
                Debug.WriteLine($"{source}: Found matching node at {match.Location}");

                // If there was no match (only the Location object will be null because it's a struct),
                // create a new node at this location. Make sure to increment the next available ID,
                // as well as adding the new node to the list of nodes.
                if (match.Location == null)
                {
                    match = new Node(location, id++);
                    AddNode(match);
                    Debug.WriteLine($"{source}: Created new node {match.Id} at {match.Location}");
                }

                fixedNodes++;

                return match;
            }

            // Go through each link and find links with missing From/To node IDs.
            // (Missing is defined as equal to int.MinValue, and must be set as this
            // in the corresponding reader function.)
            foreach (var link in Links.Values)
            {
                // If FromNodeId is missing, fix it.
                if (link.FromNodeId == int.MinValue)
                {
                    // First, find its position (the first point in the geometry)
                    var location = link.Geometry.Points.First();

                    // Set FromNodeId to the matching node (either one we found, or one we created).
                    link.FromNodeId = FindMatchingNode(location, "FromNodeId, link " + link.LinkId).Id;
                }

                // Repeat for ToNodeId
                if (link.ToNodeId == int.MinValue)
                {
                    // First, find its position (the first point in the geometry)
                    var location = link.Geometry.Points.Last();

                    // Set FromNodeId to the matching node (either one we found, or one we created).
                    link.ToNodeId = FindMatchingNode(location, "ToNodeId, link " + link.LinkId).Id;
                }
            }

            return fixedNodes;
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
                    Reference = properties.Value<double>("FROM_M") + "-" + properties.Value<double>("ROUTEID") + "@" + properties.Value<double>("TO_M"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<int>("FT_Fart"),
                    SpeedLimitReversed = properties.Value<int>("TF_Fart"),
                    FromRelativeLength = properties.Value<double>("FROM_M"),
                    ToRelativeLength = properties.Value<double>("TO_M"),
                    Geometry = new PolyLineZ(coordinates.Select(p => new Point3D(p[0], p[1], p[2])), true),
                    Raw = feature
                };

                var direction = properties.Value<string>("ONEWAY");
                if (direction != "B")
                {
                    if (direction == "FT") data.ReverseCost = double.MaxValue;
                    else if (direction == "TF") data.Cost = double.MaxValue;
                    else if (direction == "N") data.ReverseCost = data.Cost = double.MaxValue;
                }

                return data;
            }));
        }

        public static RoadNetworkRouter LoadFrom(string file, Action<int, int> progress = null)
        {
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                var router = new RoadNetworkRouter();

                var vertexCount = reader.ReadInt32();

                var itemSize = 4 + 8 + 8 + 4;
                var buffer = new byte[vertexCount * itemSize];
                reader.Read(buffer, 0, buffer.Length);
                for (var i = 0; i < vertexCount; i++)
                {
                    var pos = i * itemSize;
                    var id = BitConverter.ToInt32(buffer, pos);
                    router.Vertices.Add(id, new NetworkNode(BitConverter.ToDouble(buffer, pos + 4), BitConverter.ToDouble(buffer, pos + 12), id) { Edges = BitConverter.ToInt32(buffer, pos + 20) });
                }

                var linkCount = reader.ReadInt32();
                for (var i = 0; i < linkCount; i++)
                {
                    var link = new GdbRoadLinkData();
                    link.Reference = reader.ReadString();
                    link.Direction = reader.ReadString();
                    link.RoadType = reader.ReadString();
                    link.SpecialRoad = reader.ReadString();
                    link.LaneCode = reader.ReadString();
                    var pointCount = reader.ReadInt32();
                    itemSize = 4 + 4 + 8 + 8 + 4 + 4 + 4 + 4 + 4 + 8 + 8 + 4 + 4 + pointCount * (8 + 8 + 8);
                    buffer = new byte[itemSize];
                    reader.Read(buffer, 0, buffer.Length);

                    var pos = 0;
                    link.RoadClass = BitConverter.ToInt32(buffer, pos);
                    link.LinkId = BitConverter.ToInt32(buffer, pos + 4);
                    link.FromRelativeLength = BitConverter.ToDouble(buffer, pos + 8);
                    link.ToRelativeLength = BitConverter.ToDouble(buffer, pos + 16);
                    link.FromNodeId = BitConverter.ToInt32(buffer, pos + 24);
                    link.ToNodeId = BitConverter.ToInt32(buffer, pos + 28);
                    link.RoadNumber = BitConverter.ToInt32(buffer, pos + 32);
                    link.SpeedLimit = BitConverter.ToInt32(buffer, pos + 36);
                    link.SpeedLimitReversed = BitConverter.ToInt32(buffer, pos + 40);
                    link.Cost = BitConverter.ToDouble(buffer, pos + 44);
                    link.ReverseCost = BitConverter.ToDouble(buffer, pos + 52);
                    link.FromNodeConnectionTolerance = BitConverter.ToInt32(buffer, pos + 60);
                    link.ToNodeConnectionTolerance = BitConverter.ToInt32(buffer, pos + 64);

                    if (link.Direction != "B")
                    {
                        if (link.Direction == "FT") link.ReverseCost = double.MaxValue;
                        else if (link.Direction == "TF") link.Cost = double.MaxValue;
                        else if (link.Direction == "N") link.ReverseCost = link.Cost = double.MaxValue;
                    }

                    pos = 68;
                    var points = new Point3D[pointCount];
                    for (var j = 0; j < pointCount; j++)
                    {
                        points[j] = new Point3D(BitConverter.ToDouble(buffer, pos), BitConverter.ToDouble(buffer, pos + 8), BitConverter.ToDouble(buffer, pos + 16));
                        pos += 24;
                    }

                    link.Geometry = new PolyLineZ(points, false);

                    router.Links.Add(link.LinkId, link);

                    progress?.Invoke(i, linkCount);
                }

                return router;
            }
        }

        public void SaveTo(string file)
        {
            using (var writer = new BinaryWriter(File.Create(file)))
            {
                writer.Write(Vertices.Count);
                foreach (var v in Vertices.Values)
                {
                    writer.Write(v.Id);
                    writer.Write(v.X);
                    writer.Write(v.Y);
                    writer.Write(v.Edges);
                }

                writer.Write(Links.Count);
                foreach (var link in Links.Values)
                {
                    writer.Write(link.Reference);
                    writer.Write(link.Direction);
                    writer.Write(link.RoadType);
                    writer.Write(link.SpecialRoad);
                    writer.Write(link.LaneCode ?? "");

                    writer.Write(link.Geometry.Points.Length);

                    writer.Write(link.RoadClass);
                    writer.Write(link.LinkId);
                    writer.Write(link.FromRelativeLength);
                    writer.Write(link.ToRelativeLength);
                    writer.Write(link.FromNodeId);
                    writer.Write(link.ToNodeId);
                    writer.Write(link.RoadNumber);
                    writer.Write(link.SpeedLimit);
                    writer.Write(link.SpeedLimitReversed);
                    writer.Write(link.Cost);
                    writer.Write(link.ReverseCost);
                    writer.Write(link.FromNodeConnectionTolerance);
                    writer.Write(link.ToNodeConnectionTolerance);

                    foreach (var p in link.Geometry.Points)
                    {
                        writer.Write(p.X);
                        writer.Write(p.Y);
                        writer.Write(p.Z);
                    }
                }
            }
        }

        public static void SaveToLight(string file, Dictionary<int, NetworkNode> nodes, Dictionary<string, LightGdbRoadLinkData> links, IEnumerable<GdbRoadLinkData> streamedLinks)
        {
            using (var writer = new BinaryWriter(File.Create(file)))
            {
                writer.Write(nodes.Count);
                foreach (var v in nodes.Values)
                {
                    writer.Write(v.Id);
                    writer.Write(v.X);
                    writer.Write(v.Y);
                    writer.Write(v.Edges);
                }

                writer.Write(links.Count);
                foreach (var link in streamedLinks)
                {
                    var lightLink = links[link.Reference];

                    writer.Write(link.Reference);
                    writer.Write(link.Direction);
                    writer.Write(link.RoadType);
                    writer.Write(link.SpecialRoad);
                    writer.Write(link.LaneCode ?? "");

                    writer.Write(link.Geometry.Points.Length);

                    writer.Write(link.RoadClass);
                    writer.Write(link.LinkId);
                    writer.Write(link.FromRelativeLength);
                    writer.Write(link.ToRelativeLength);
                    writer.Write(lightLink.FromNodeId); // *
                    writer.Write(lightLink.ToNodeId); // *
                    writer.Write(link.RoadNumber);
                    writer.Write(link.SpeedLimit);
                    writer.Write(link.SpeedLimitReversed);
                    writer.Write(link.Cost);
                    writer.Write(link.ReverseCost);
                    writer.Write(lightLink.FromNodeConnectionTolerance); // *
                    writer.Write(lightLink.ToNodeConnectionTolerance); // *

                    foreach (var p in link.Geometry.Points)
                    {
                        writer.Write(p.X);
                        writer.Write(p.Y);
                        writer.Write(p.Z);
                    }
                }
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

        public void ExportNodes(string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Point);

            var table = shp.DataTable;
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Edges", typeof(int));
            table.AcceptChanges();

            foreach (var c in Vertices.Values)
            {
                var feature = shp.AddFeature(new Point(new Coordinate(c.X, c.Y)));
                feature.DataRow["Id"] = c.Id;
                feature.DataRow["Edges"] = c.Edges;
            }

            shp.SaveAs(shpPath, true);
        }

        public void ExportLinks(string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Line);

            var table = shp.DataTable;
            table.Columns.Add("Reference", typeof(string));
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("FromNode", typeof(int));
            table.Columns.Add("ToNode", typeof(int));
            table.Columns.Add("FromTolr.", typeof(int));
            table.Columns.Add("ToTolr.", typeof(int));
            table.Columns.Add("Speed", typeof(int));
            table.Columns.Add("Speed rev.", typeof(int));
            table.Columns.Add("Cost", typeof(double));
            table.Columns.Add("Cost rev.", typeof(double));
            table.AcceptChanges();

            foreach (var c in Links.Values)
            {
                var feature = shp.AddFeature(new LineString(c.Geometry.Points.Select(p => new Coordinate(p.X, p.Y))));
                feature.DataRow["FromNode"] = c.FromNodeId;
                feature.DataRow["ToNode"] = c.ToNodeId;
                feature.DataRow["Id"] = c.LinkId;
                feature.DataRow["Reference"] = c.Reference;
                feature.DataRow["FromTolr."] = c.FromNodeConnectionTolerance;
                feature.DataRow["ToTolr."] = c.ToNodeConnectionTolerance;
                feature.DataRow["Cost"] = c.Cost;
                feature.DataRow["Cost rev."] = c.ReverseCost;
                feature.DataRow["Speed"] = c.SpeedLimit;
                feature.DataRow["Speed rev."] = c.SpeedLimitReversed;
            }

            shp.SaveAs(shpPath, true);
        }

        public void ExportNodeConnections(string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Point);

            var table = shp.DataTable;
            table.Columns.Add("LinkId", typeof(int));
            table.Columns.Add("NodeId", typeof(int));
            table.Columns.Add("FromOrTo", typeof(char));
            table.Columns.Add("Distance", typeof(double));
            table.Columns.Add("Tolerance", typeof(int));
            table.AcceptChanges();

            foreach (var c in Links.Values)
            {
                var node = Vertices[c.FromNodeId];
                var coordinate = c.Geometry.Points.First();
                var feature = shp.AddFeature(new LineString(new[] { new Coordinate(node.X, node.Y), new Coordinate(coordinate.X, coordinate.Y) }));
                feature.DataRow["LinkId"] = c.LinkId;
                feature.DataRow["NodeId"] = node.Id;
                feature.DataRow["FromOrTo"] = 0;
                feature.DataRow["Distance"] = coordinate.DistanceTo2D(node.X, node.Y);
                feature.DataRow["Tolerance"] = c.FromNodeConnectionTolerance;

                node = Vertices[c.ToNodeId];
                coordinate = c.Geometry.Points.Last();
                feature = shp.AddFeature(new LineString(new[] { new Coordinate(node.X, node.Y), new Coordinate(coordinate.X, coordinate.Y) }));
                feature.DataRow["LinkId"] = c.LinkId;
                feature.DataRow["NodeId"] = node.Id;
                feature.DataRow["FromOrTo"] = 2;
                feature.DataRow["Distance"] = coordinate.DistanceTo2D(node.X, node.Y);
                feature.DataRow["Tolerance"] = c.ToNodeConnectionTolerance;
            }

            shp.SaveAs(shpPath, true);
        }

        public (GdbRoadLinkData Link, NearestPointInfo Nearest) GetNearestVertexFromNearestEdge(Point3D point)
        {
            (GdbRoadLinkData Link, NearestPointInfo Nearest) nearest = (null, null);
            var d = 1000;
            while (nearest.Link == null)
            {
                nearest = Links
                    .Values
                    .Where(p => Math.Abs(p.Geometry.Points[0].Y - point.Y) < d && Math.Abs(p.Geometry.Points[0].X - point.X) < d)
                    .Select(p => (Link: p, Nearest: LineTools.FindNearestPoint(p.Geometry.Points, point.X, point.Y)))
                    .MinBy(p => p.Nearest.DistanceFromLine);
                d *= 10;
            }

            return nearest;
        }

        public (QuickGraphSearchResult route, GdbRoadLinkData[] links) Search(Point3D fromPoint, Point3D toPoint)
        {
            var source = GetNearestVertexFromNearestEdge(fromPoint);
            var target = GetNearestVertexFromNearestEdge(toPoint);

            // Build a network graph for searching
            var graph = GetGraph();

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

            return (route, links);
        }

        private GdbRoadLinkData CutLink(GdbRoadLinkData current, GdbRoadLinkData connectedTo,  double atDistance)
        {
            var connectedAtEnd = current.ToNodeId == connectedTo.FromNodeId || current.ToNodeId == connectedTo.ToNodeId;

            Point3D[] points;
            if(connectedAtEnd)
                points = LineTools.CutStart(current.Geometry.Points, atDistance);
            else
                points = LineTools.CutEnd(current.Geometry.Points, current.Geometry.Length - atDistance);

            return current.Clone(new PolyLineZ(points, false));
        }
    }
}