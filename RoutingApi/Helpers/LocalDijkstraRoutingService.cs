using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.IEnumerableExtensions;
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

        private static object _lockObject = new object();
        private static Graph _graph = null;
        private static Dictionary<long, InternalLinkRepresentation> _links = null;
        private static Dictionary<int, InternalVertexRepresentation> _vertices;
        private static GraphAnalysis _analysis;
        public static string NetworkFile { get; set; }

        public static RoutingResponse FromLatLng(List<RequestCoordinate> coordinates)
        {
            var utmCoordinates = coordinates.Select(p => p.GetUtm33()).ToArray();
            return FromUtm(utmCoordinates);
        }

        private struct InternalLinkRepresentation
        {
            public Point3D[] Geometry;
            public GraphDataItem Edge;
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
                    _vertices = vertices.ToDictionary(k => k.Id, v => v);
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

                // Find nearest vertices:
                var fromVertex = GetNearestVertexFromNearestEdge(fromCoord.X, fromCoord.Y);
                var toVertex = GetNearestVertexFromNearestEdge(toCoord.X, toCoord.Y);

                // If these vertices are in different unconnected parts of the network, find the best of these parts to use, and
                // route to the nearest point in that part instead of in the entire network.
                if (_analysis.VertexIdGroup[fromVertex.Vertex.Id] != _analysis.VertexIdGroup[toVertex.Vertex.Id])
                {
                    // Locate the alternative vertices; for each already found nearest vertices, find the vertex in the respective part of the network
                    // that is nearest to the other vertex.
                    var alternativeToVertex = GetNearestVertexFromNearestEdge(toCoord.X, toCoord.Y, _analysis.VertexIdGroup[fromVertex.Vertex.Id]);
                    var alternativeFromVertex = GetNearestVertexFromNearestEdge(fromCoord.X, fromCoord.Y, _analysis.VertexIdGroup[toVertex.Vertex.Id]);

                    // Then calculate the distance between the alternative vertices and the actually nearest vertices
                    var toDistance = Distance(alternativeToVertex.Vertex.X, alternativeToVertex.Vertex.Y, toCoord.X, toCoord.Y);
                    var fromDistance = Distance(alternativeFromVertex.Vertex.X, alternativeFromVertex.Vertex.Y, fromCoord.X, fromCoord.Y);

                    // And pick the vertex from the network part that in total gives the least "lost" distance.
                    if (toDistance < fromDistance)
                        toVertex = alternativeToVertex;
                    else
                        fromVertex = alternativeFromVertex;
                }

                var start = DateTime.Now;

                var path = _graph.GetShortestPath(fromVertex.Vertex.Id, toVertex.Vertex.Id).Items;

                rs.SecsDijkstra += DateTime.Now.Subtract(start).TotalSeconds;
                start = DateTime.Now;

                if (!path.Any()) throw new Exception("Couldn't find a route between these points.");

                rs.SecsRetrieveLinks += DateTime.Now.Subtract(start).TotalSeconds;
                start = DateTime.Now;

                //TODO: Use RoadNetworkRouter which now has built-in support for searching from points on links

                var sortedLinks = path.Select(p => _links[p]).ToList();
                /*if (sortedLinks.First().Edge.Id != fromVertex.Link.Edge.Id)
                    sortedLinks.Insert(0, fromVertex.Link);
                if (sortedLinks.Last().Edge.Id != toVertex.Link.Edge.Id)
                    sortedLinks.Add(toVertex.Link);*/

                var prevPoint = fromCoord;
                for (var linkIx = 0; linkIx < sortedLinks.Count; linkIx++)
                {
                    var link = sortedLinks[linkIx];
                    var reversed = Distance(link.Geometry[0].X, link.Geometry[0].Y, prevPoint.X, prevPoint.Y) > Distance(link.Geometry[^1].X, link.Geometry[^1].Y, prevPoint.X, prevPoint.Y);
                    var geometry = (reversed ? link.Geometry.Reverse() : link.Geometry).Select(p => new Point3D(p.X, p.Y)).ToArray();

                    link.Reference.Direction = reversed ? 2 : 1;

                    if (linkIx == 0 || linkIx == sortedLinks.Count - 1)
                    {
                        Point3D[] modifiedGeometry = null;
                        if (linkIx == 0)
                        {
                            modifiedGeometry = LineTools.CutEndAt(geometry, fromCoord.X, toCoord.Y);
                        }else if (linkIx == sortedLinks.Count - 1)
                        {
                            modifiedGeometry = LineTools.CutStartAt(geometry, fromCoord.X, toCoord.Y);
                        }

                        if (modifiedGeometry?.Any() == true)
                            geometry = modifiedGeometry;
                    }

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
            var dx = x1 - x2;
            var dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static (InternalLinkRepresentation Link, InternalVertexRepresentation Vertex) GetNearestVertexFromNearestEdge(double x, double y)
        {
            var nearest = new InternalLinkRepresentation();
            var d = 1000;
            while (nearest.Edge == null)
            {
                nearest = _links
                    .Values
                    .Where(p => Math.Abs(p.Geometry[0].Y - y) < d && Math.Abs(p.Geometry[0].X - x) < d)
                    .MinBy(p => LineTools.FindNearestPoint(p.Geometry, x, y).DistanceFromLine);
                d *= 10;
            }

            return CreateNearestInfo(nearest, x, y);
        }

        private static (InternalLinkRepresentation Link, InternalVertexRepresentation Vertex) CreateNearestInfo(InternalLinkRepresentation nearest, double x, double y)
        {
            var distanceToStart = Distance(nearest.Geometry[0].X, nearest.Geometry[0].Y, x, y);
            var distanceToEnd = Distance(nearest.Geometry[^1].X, nearest.Geometry[^1].Y, x, y);

            if (distanceToStart < distanceToEnd)
                return (nearest, _vertices[nearest.Edge.SourceVertexId]);
            else
                return (nearest, _vertices[nearest.Edge.TargetVertexId]);
        }

        private static (InternalLinkRepresentation Link, InternalVertexRepresentation Vertex) GetNearestVertexFromNearestEdge(double x, double y, int vertexGroup)
        {
            var d = 1000;
            var nearest = _links.Values
                .Where(p => _analysis.VertexIdGroup.TryGetValue(p.Edge.SourceVertexId, out var gidS) && gidS == vertexGroup && _analysis.VertexIdGroup.TryGetValue(p.Edge.SourceVertexId, out var gidE) && gidE == vertexGroup)
                .Where(p => Math.Abs(p.Geometry[0].Y - y) < d && Math.Abs(p.Geometry[0].X - x) < d)
                .MinBy(p => p.Geometry.Min(c => Distance(c.X, c.Y, x, y)));

            return CreateNearestInfo(nearest, x, y);
        }

        public static int GetNearestVertex(double x, double y)
        {
            var nearest = _vertices.Values.MinBy(p => Distance(p.X, p.Y, x, y));
            return nearest.Id;
        }

        private static InternalVertexRepresentation GetNearestVertex(double x, double y, int vertexGroup)
        {
            var nearest = _vertices
                .Values
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
                        Reference = LinkReference.FromShortStringRepresentation(link.Id),
                        Geometry = new Point3D[pointCount],
                        Edge = link
                    };

                    for (var j = 0; j < pointCount; j++)
                    {
                        edge.Geometry[j] = new Point3D(BitConverter.ToDouble(buffer, pos), BitConverter.ToDouble(buffer, pos + 8));
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