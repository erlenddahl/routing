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
                    Reference = properties.Value<double>("FROM_M") + "-" + properties.Value<double>("ROUTEID") + "@" + properties.Value<double>("TO_M"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<int>("FT_Fart"),
                    SpeedLimitReversed = properties.Value<int>("TF_Fart"),
                    FromRelativeLength = properties.Value<double>("FROM_M"),
                    ToRelativeLength = properties.Value<double>("TO_M"),
                    Geometry = new PolyLineZ(coordinates.Select(p => new Point3D(p[0], p[1], p[2])), true)
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
                    LinkId = properties.Value<int>("OBJECTID"),
                    Reference = properties.Value<double>("from_measure") + "-" + properties.Value<double>("to_measure") + "@" + properties.Value<double>("routeid"),
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    //RoadType = properties.Value<string>("VEGTYPE"),
                    SpeedLimit = properties.Value<int>("speedfw"),
                    SpeedLimitReversed = properties.Value<int>("speedbw"),
                    Cost = properties.Value<double>("drivetime_fw"),
                    ReverseCost = properties.Value<double>("drivetime_bw"),
                    FromRelativeLength = properties.Value<double>("from_measure"),
                    ToRelativeLength = properties.Value<double>("to_measure"),
                    Geometry = new PolyLineZ(coordinates.Select(p => new{Utm=wgs84ToUtm33(p[0], p[1]), Z=p[2]}).Select(p => new Point3D(p.Utm.X, p.Utm.Y, p.Z)), true),
                    RoadClass = properties.Value<int>("roadtype")
                };
                
                data.Direction = GdbRoadLinkData.DirectionFromString(properties.Value<string>("oneway"));

                if (data.Direction != RoadLinkDirection.BothWays)
                {
                    if (data.Direction == RoadLinkDirection.AlongGeometry) data.ReverseCost = double.MaxValue;
                    else if (data.Direction == RoadLinkDirection.AgainstGeometry) data.Cost = double.MaxValue;
                    else if (data.Direction == RoadLinkDirection.None) data.ReverseCost = data.Cost = double.MaxValue;
                }

                data.Direction = data.Direction;

                return data;
            }));
        }

        /// <summary>
        /// Loads the road network from a road network binary file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
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

                    // Read the dynamic strings directly first
                    link.Reference = reader.ReadString();
                    link.RoadType = reader.ReadString();
                    link.LaneCode = reader.ReadString();

                    // Then fetch the point count, and calculate the length of the rest of this link object, and read it all in at the same time
                    var pointCount = reader.ReadInt32();
                    itemSize = 4 + 4 + 4 + 8 + 8 + 4 + 4 + 4 + 4 + 4 + 8 + 8 + pointCount * (8 + 8 + 8);
                    buffer = new byte[itemSize];
                    reader.Read(buffer, 0, buffer.Length);

                    // Read all the normal properties
                    var pos = 0;
                    link.Direction = (RoadLinkDirection)BitConverter.ToInt32(buffer, pos);
                    link.RoadClass = BitConverter.ToInt32(buffer, pos + 4);
                    link.LinkId = BitConverter.ToInt32(buffer, pos + 8);
                    link.FromRelativeLength = BitConverter.ToDouble(buffer, pos + 12);
                    link.ToRelativeLength = BitConverter.ToDouble(buffer, pos + 20);
                    link.FromNodeId = BitConverter.ToInt32(buffer, pos + 28);
                    link.ToNodeId = BitConverter.ToInt32(buffer, pos + 32);
                    link.RoadNumber = BitConverter.ToInt32(buffer, pos + 36);
                    link.SpeedLimit = BitConverter.ToInt32(buffer, pos + 40);
                    link.SpeedLimitReversed = BitConverter.ToInt32(buffer, pos + 44);
                    link.Cost = BitConverter.ToDouble(buffer, pos + 48);
                    link.ReverseCost = BitConverter.ToDouble(buffer, pos + 56);

                    // Update the position to the end of the normal properties, and read all points
                    pos = 64;
                    var points = new Point3D[pointCount];
                    for (var j = 0; j < pointCount; j++)
                    {
                        points[j] = new Point3D(BitConverter.ToDouble(buffer, pos), BitConverter.ToDouble(buffer, pos + 8), BitConverter.ToDouble(buffer, pos + 16));
                        pos += 24;
                    }

                    // Initialize a polyline from the read points
                    link.Geometry = new PolyLineZ(points, false);

                    router.Links.Add(link.LinkId, link);

                    progress?.Invoke(i, linkCount);
                }

                return router;
            }
        }

        /// <summary>
        /// Writes the network to a fast binary file.
        /// </summary>
        /// <param name="file"></param>
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
                    writer.Write(link.RoadType);
                    writer.Write(link.LaneCode ?? "");

                    writer.Write(link.Geometry.Points.Length);

                    writer.Write((int)link.Direction);
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

            // If the cut failed, simply return the entire link.
            // This is a workaround for now, should figure out what makes it fail later.
            if (points.Length < 2)
                return current;

            return current.Clone(new PolyLineZ(points, false));
        }
    }
}