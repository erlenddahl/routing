using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions.IEnumerableExtensions;
using Routing;
using RoutingApi.Geometry;

namespace RoutingApi.Helpers
{
    public class LocalDijkstraRoutingService
    {
        private LocalDijkstraRoutingService()
        {
        }

        private static object _lockObject = new object();
        private static Graph _graph = null;
        private static Dictionary<long, InternalLinkRepresentation> _links = null;
        private static InternalVertexRepresentation[] _vertices;
        private static GraphAnalysis _analysis;
        public static string NetworkFile { get; set; }

        public static RoutingResponse FromLatLng(List<LatLng> coordinates)
        {
            var utmCoordinates = coordinates.Select(p => new PointWgs84(p.Lat, p.Lng).ToUtm33()).ToArray();
            return FromUtm(utmCoordinates);
        }

        private struct FloatPoint
        {
            public float X;
            public float Y;

            public FloatPoint(float x, float y)
            {
                X = x;
                Y = y;
            }
        }

        private struct InternalLinkRepresentation
        {
            public FloatPoint[] Geometry;
            public long EdgeId;
            public LinkReference Reference;
        }

        private struct InternalVertexRepresentation
        {
            public int Id { get; set; }
            public float X{ get; set; }
            public float Y { get; set; }

            public InternalVertexRepresentation(int id, double x, double y)
            {
                Id = id;
                X = (float)x;
                Y = (float)y;
            }
        }

        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_graph == null)
                {
                    var (links, vertices, graphItems) = LoadFrom(NetworkFile);
                    _graph = Graph.Create(graphItems);
                    _analysis = _graph.Analyze();

                    _links = links;
                    _vertices = vertices;
                }
            }
        }

        public static RoutingResponse FromUtm(PointUtm33[] coordinates)
        {
            Initialize();

            var rs = new RoutingResponse
            {
                WayPoints = coordinates
            };

            for (var i = 1; i < coordinates.Length; i++)
            {
                var fromCoord = coordinates[i - 1];
                var toCoord = coordinates[i];
                rs.WayPointIndices.Add(new WayPointIndex()
                {
                    CoordinateIndex = rs.Route.Count,
                    LinkReferenceIndex = rs.LinkReferences.Count
                });

                // Find nearest vertices:
                var fromVertex = GetNearestVertex(fromCoord.X, fromCoord.Y);
                var toVertex = GetNearestVertex(toCoord.X, toCoord.Y);

                // If these vertices are in different unconnected parts of the network, find the best of these parts to use, and
                // route to the nearest point in that part instead of in the entire network.
                if (_analysis.VertexIdGroup[fromVertex] != _analysis.VertexIdGroup[toVertex])
                {
                    // Locate the alternative vertices; for each already found nearest vertices, find the vertex in the respective part of the network
                    // that is nearest to the other vertex.
                    var alternativeToVertex = GetNearestVertex(toCoord.X, toCoord.Y, _analysis.VertexIdGroup[fromVertex]);
                    var alternativeFromVertex = GetNearestVertex(fromCoord.X, fromCoord.Y, _analysis.VertexIdGroup[toVertex]);

                    // Then calculate the distance between the alternative vertices and the actually nearest vertices
                    var toDistance = Distance(alternativeToVertex.X, alternativeToVertex.Y, toCoord.X, toCoord.Y);
                    var fromDistance = Distance(alternativeFromVertex.X, alternativeFromVertex.Y, fromCoord.X, fromCoord.Y);

                    // And pick the vertex from the network part that in total gives the least "lost" distance.
                    if (toDistance < fromDistance)
                        toVertex = alternativeToVertex.Id;
                    else
                        fromVertex = alternativeFromVertex.Id;
                }

                var start = DateTime.Now;

                int[] path;
                lock (_lockObject)
                {
                    path = _graph.GetShortestPath((int) fromVertex, (int) toVertex).Select(p => p.EdgeId).ToArray();
                }

                rs.SecsDijkstra += DateTime.Now.Subtract(start).TotalSeconds;
                start = DateTime.Now;

                if (!path.Any()) throw new Exception("Couldn't find a route between these points.");

                rs.SecsRetrieveLinks += DateTime.Now.Subtract(start).TotalSeconds;
                start = DateTime.Now;

                var sortedLinks = path.Select(p => _links[p]).ToArray();

                var prevPoint = fromCoord;
                foreach (var link in sortedLinks)
                {
                    var reversed = Distance(link.Geometry[0].X, link.Geometry[0].Y, prevPoint.X, prevPoint.Y) > Distance(link.Geometry[link.Geometry.Length - 1].X, link.Geometry[link.Geometry.Length - 1].Y, prevPoint.X, prevPoint.Y);
                    var geometry = reversed ? link.Geometry.Reverse() : link.Geometry;

                    link.Reference.Direction = reversed ? 2 : 1;

                    rs.LinkReferences.Add(link.Reference);
                    rs.Route.AddRange(geometry.Select(p => new PointUtm33(p.X, p.Y, 0)));

                    prevPoint = rs.Route.Last();
                }

                rs.SecsModifyLinks += DateTime.Now.Subtract(start).TotalSeconds;
            }

            return rs;
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        public static int GetNearestVertex(double x, double y)
        {
            var nearest = _vertices.MinBy(p => Distance(p.X, p.Y, x, y));
            return nearest.Id;
        }

        private static InternalVertexRepresentation GetNearestVertex(double x, double y, int vertexGroup)
        {
            var nearest = _vertices
                .Where(p => _analysis.VertexIdGroup.TryGetValue(p.Id, out var gid) && gid == vertexGroup)
                .MinBy(p => Distance(p.X, p.Y, x, y));
            return nearest;
        }

        private static (Dictionary<long, InternalLinkRepresentation> links, InternalVertexRepresentation[] vertices, GraphDataItem[] graphItems) LoadFrom(string file)
        {
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                var vertexCount = reader.ReadInt32();

                var vertices = new InternalVertexRepresentation[vertexCount];

                var itemSize = 4 + 8 + 8 + 4;
                var buffer = new byte[vertexCount * itemSize];
                reader.Read(buffer, 0, buffer.Length);
                for (var i = 0; i < vertexCount; i++)
                {
                    var pos = i * itemSize;
                    var id = BitConverter.ToInt32(buffer, pos);
                    vertices[i] = new InternalVertexRepresentation(id, BitConverter.ToDouble(buffer, pos + 4), BitConverter.ToDouble(buffer, pos + 12));
                }

                var linkCount = reader.ReadInt32();
                var links = new Dictionary<long, InternalLinkRepresentation>(linkCount);
                var graphItems = new GraphDataItem[linkCount];
                for (var i = 0; i < linkCount; i++)
                {
                    var link = new GraphDataItem();
                    link.Id = reader.ReadString();
                    var direction = reader.ReadString(); // link.Direction
                    reader.ReadString(); // link.RoadType
                    reader.ReadString(); // link.SpecialRoad
                    reader.ReadString(); // link.LaneCode
                    var pointCount = reader.ReadInt32();
                    itemSize = 4 + 4 + 8 + 8 + 4 + 4 + 4 + 4 + 4 + 8 + 8 + 4 + 4 + pointCount * (8 + 8 + 8);
                    buffer = new byte[itemSize];
                    reader.Read(buffer, 0, buffer.Length);

                    var pos = 0;
                    //link.RoadClass = BitConverter.ToInt32(buffer, pos);
                    link.EdgeId = BitConverter.ToInt32(buffer, pos + 4);
                    //link.FromRelativeLength = BitConverter.ToDouble(buffer, pos + 8);
                    //link.ToRelativeLength = BitConverter.ToDouble(buffer, pos + 16);
                    link.SourceVertexId = BitConverter.ToInt32(buffer, pos + 24);
                    link.TargetVertexId = BitConverter.ToInt32(buffer, pos + 28);
                    //link.RoadNumber = BitConverter.ToInt32(buffer, pos + 32);
                    //link.SpeedLimit = BitConverter.ToInt32(buffer, pos + 36);
                    //link.SpeedLimitReversed = BitConverter.ToInt32(buffer, pos + 40);
                    link.Cost = BitConverter.ToDouble(buffer, pos + 44);
                    link.ReverseCost = BitConverter.ToDouble(buffer, pos + 52);
                    //link.FromNodeConnectionTolerance = BitConverter.ToInt32(buffer, pos + 60);
                    //link.ToNodeConnectionTolerance = BitConverter.ToInt32(buffer, pos + 64);

                    if (direction != "B")
                    {
                        if (direction == "FT") link.ReverseCost = double.MaxValue;
                        else if (direction == "TF") link.Cost = double.MaxValue;
                        else if (direction == "N") link.ReverseCost = link.Cost = double.MaxValue;
                        else
                        {
                            var k = 0;
                        }
                    }

                    pos = 68;

                    var edge = new InternalLinkRepresentation()
                    {
                        EdgeId = link.EdgeId,
                        Reference = LinkReference.FromShortStringRepresentation(link.Id),
                        Geometry = new FloatPoint[pointCount]
                    };

                    for (var j = 0; j < pointCount; j++)
                    {
                        edge.Geometry[j] = new FloatPoint((float)BitConverter.ToDouble(buffer, pos), (float)BitConverter.ToDouble(buffer, pos + 8));
                        pos += 24;
                    }

                    links[i] = edge;
                    graphItems[i] = link;
                }

                return (links, vertices, graphItems);
            }
        }
    }
}