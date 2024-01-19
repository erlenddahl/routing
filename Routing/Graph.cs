using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Routing
{
    public class Graph<T>
    {
        public static string Version = "2023-05-29";

        private readonly Dictionary<long, Edge<T>> _edges = new();
        public Dictionary<int, Vertex> Vertices { get; } = new();
        public int EdgeCount => _edges.Count;

        public static Graph<GraphDataItem> Create(IEnumerable<GraphDataItem> items)
        {
            var graph = new Graph<GraphDataItem>();
            foreach (var item in items)
            {
                graph.CreateEdge(item, item.SourceVertexId, item.TargetVertexId, item.Cost, item.ReverseCost);
            }
            return graph;
        }

        public Graph(){}

        public GraphAnalysis<T> Analyze()
        {
            return new GraphAnalysis<T>(this);
        }

        public QuickGraphSearchResult<T> GetShortestPath(int sourceVertexId, int targetVertexId, GraphOverloader<T> overloader = null, double maxCost = double.MaxValue)
        {
            var dr = Dijkstra.GetShortestPath(this, sourceVertexId, targetVertexId, overloader, maxCost);
            var result = new QuickGraphSearchResult<T>(dr);

            return result;
        }

        public IEnumerable<QuickGraphSearchResult<T>> GetShortestPathToAll(int sourceVertexId, HashSet<int> relevantVertices = null)
        {
            var dr = Dijkstra.GetShortestPath(this, sourceVertexId, -1);

            var source = Vertices[sourceVertexId];
            foreach (var v in Vertices)
            {
                if (relevantVertices?.Contains(v.Value.Id) == false)
                {
                    continue;
                }
                if (v.Value == dr.Target.Vertex || !dr.HasVisitedVertex(v.Key))
                {
                    yield return null;
                    continue;
                }
                yield return new QuickGraphSearchResult<T>(dr);
            }
        }

        public IEnumerable<Vertex> GetShortestPathToAllVertices(int sourceVertexId)
        {
            var dr = Dijkstra.GetShortestPath(this, sourceVertexId, -1);

            foreach (var v in Vertices)
            {
                if (v.Value == dr.Target.Vertex || !dr.HasVisitedVertex(v.Key))
                {
                    yield return null;
                    continue;
                }
                yield return v.Value;
            }
        }

        public void CreateEdge(T item, int fromNodeId, int toNodeId, double cost, double reverseCost)
        {
            if (cost < 1_000_000)
            {
                var edge = new Edge<T>
                {
                    SourceVertex = EnsureVertex(fromNodeId),
                    TargetVertex = EnsureVertex(toNodeId),
                    Cost = (float)cost,
                    IsReverse = false,
                    DataItem = item,
                };

                EnsureEdge(edge);
            }

            if (reverseCost < 1_000_000)
            {
                var edge = new Edge<T>
                {
                    SourceVertex = EnsureVertex(toNodeId),
                    TargetVertex = EnsureVertex(fromNodeId),
                    Cost = (float)reverseCost,
                    IsReverse = true,
                    DataItem = item,
                };

                EnsureEdge(edge);
            }
        }
        
        private void EnsureEdge(Edge<T> edge)
        {
            edge.SourceVertex.NeighbourIds.Add(edge.TargetVertex.Id);

            var key = GetKey(edge.SourceVertex.Id, edge.TargetVertex.Id);
            if (_edges.TryGetValue(key, out var existing))
            {
                if (existing.Cost > edge.Cost)
                    _edges[key] = edge;
            }
            else
                _edges.Add(key, edge);
        }

        private Vertex EnsureVertex(int vertexId)
        {
            if (Vertices.TryGetValue(vertexId, out var v))
                return v;

            var newVertex = new Vertex {Id = vertexId};

            Vertices.Add(vertexId, newVertex);

            return newVertex;
        }

        private long GetKey(int start, int end)
        {
            return start * 10_000_000_000 + end;
        }

        public Edge<T> GetEdge(int startVertexId, int endVertexId)
        {
            return _edges[GetKey(startVertexId, endVertexId)];
        }

        public bool HasEdge(int startVertexId, int endVertexId)
        {
            return _edges.ContainsKey(GetKey(startVertexId, endVertexId));
        }

        public bool TryGetEdge(int startVertexId, int endVertexId, out Edge<T> edge)
        {
            return _edges.TryGetValue(GetKey(startVertexId, endVertexId), out edge);
        }

        /*public void SaveTo(string path)
        {
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(_edges.Count);
            foreach (var item in _edges.Values.Select(p => p.DataItem))
            {
                writer.Write(item.EdgeId);
                writer.Write(item.FromNodeId);
                writer.Write(item.ToNodeId);
                writer.Write(item.Cost);
                writer.Write(item.ReverseCost);
            }
        }

        public static Graph<GraphDataItem> LoadFrom(string path)
        {
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            var count = reader.ReadInt32();

            var graph = new Graph<GraphDataItem>();
            var itemSize = sizeof(int) * 3 + sizeof(double) * 2; // Size for EdgeId, SourceVertexId, TargetVertexId, Cost, ReverseCost
            var bufferArray = new byte[itemSize];
            Span<byte>  buffer = bufferArray;

            for (var i = 0; i < count; i++)
            {
                reader.Read(buffer);
                var item = GraphDataItem.FromBytes(buffer);
                graph.CreateEdge(item);
            }

            return graph;
        }*/
    }
}