﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Routing
{
    public class Graph
    {
        private readonly Dictionary<long, Edge> _edges = new Dictionary<long, Edge>();
        public Dictionary<int, Vertex> Vertices { get; } = new Dictionary<int, Vertex>();
        public int EdgeCount => _edges.Count;

        private Graph(IEnumerable<GraphDataItem> items)
        {
            foreach (var item in items)
            {
                CreateEdge(item);
            }
        }

        public static Graph Create(IEnumerable<GraphDataItem> items)
        {
            return new Graph(items);
        }

        private Graph(){}

        public GraphAnalysis Analyze()
        {
            return new GraphAnalysis(this);
        }

        public QuickGraphSearchResult GetShortestPath(int sourceVertexId, int targetVertexId, GraphOverloader overloader = null)
        {
            var dr = Dijkstra.GetShortestPath(this, sourceVertexId, targetVertexId, overloader);
            var result = new QuickGraphSearchResult(dr);

            return result;
        }

        public IEnumerable<QuickGraphSearchResult> GetShortestPathToAll(int sourceVertexId, HashSet<int> relevantVertices = null)
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
                yield return new QuickGraphSearchResult(dr);
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


        private void CreateEdge(GraphDataItem item)
        {
            if (item.Cost < 1_000_000)
            {
                var edge = new Edge
                {
                    Id = item.EdgeId,
                    SourceVertex = EnsureVertex(item.SourceVertexId),
                    TargetVertex = EnsureVertex(item.TargetVertexId),
                    Cost = item.Cost,
                    IsReverse = false,
                    DataItem = item,
                };

                EnsureEdge(edge);
            }

            if (item.ReverseCost < 1_000_000)
            {
                var edge = new Edge
                {
                    Id = item.EdgeId,
                    SourceVertex = EnsureVertex(item.TargetVertexId),
                    TargetVertex = EnsureVertex(item.SourceVertexId),
                    Cost = item.ReverseCost,
                    IsReverse = true,
                    DataItem = item,
                };

                EnsureEdge(edge);
            }
        }

        private void EnsureEdge(Edge edge)
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

        public Edge GetEdge(int startVertexId, int endVertexId)
        {
            return _edges[GetKey(startVertexId, endVertexId)];
        }

        public bool HasEdge(int startVertexId, int endVertexId)
        {
            return _edges.ContainsKey(GetKey(startVertexId, endVertexId));
        }

        public bool TryGetEdge(int startVertexId, int endVertexId, out Edge edge)
        {
            return _edges.TryGetValue(GetKey(startVertexId, endVertexId), out edge);
        }
    }
}