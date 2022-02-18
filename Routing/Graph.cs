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

        public GraphSearchResult GetShortestPath(int sourceVertexId, int targetVertexId)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (vertex, tries) = Dijkstra.GetShortestPath(this, sourceVertexId, targetVertexId);
            stopwatch.Stop();
            var result = new GraphSearchResult(vertex, stopwatch.Elapsed, tries);

            ResetVertices();

            return result;
        }

        public QuickGraphSearchResult GetShortestPathQuickly(int sourceVertexId, int targetVertexId)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (vertex, tries) = Dijkstra.GetShortestPath(this, sourceVertexId, targetVertexId);
            stopwatch.Stop();
            var result = new QuickGraphSearchResult(Vertices[sourceVertexId], vertex);

            ResetVertices();

            return result;
        }

        public IEnumerable<QuickGraphSearchResult> GetShortestPathToAll(int sourceVertexId, HashSet<int> relevantVertices = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (vertex, tries) = Dijkstra.GetShortestPath(this, sourceVertexId, -1);
            stopwatch.Stop();

            var source = Vertices[sourceVertexId];
            foreach (var v in Vertices)
            {
                if (v.Value == vertex || v.Value.PreviousEdge == null || relevantVertices?.Contains(v.Value.Id) == false)
                {
                    yield return null;
                    continue;
                }
                yield return new QuickGraphSearchResult(source, v.Value);
            }

            ResetVertices();
        }

        public IEnumerable<Vertex> GetShortestPathToAllVertices(int sourceVertexId)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (vertex, tries) = Dijkstra.GetShortestPath(this, sourceVertexId, -1);
            stopwatch.Stop();

            foreach (var v in Vertices)
            {
                if (v.Value == vertex || v.Value.PreviousEdge == null)
                {
                    yield return null;
                    continue;
                }
                yield return v.Value;
            }

            ResetVertices();
        }

        public void ResetVertices()
        {
            foreach (var item in Vertices.Values)
            {
                item.Visited = false;
                item.Cost = double.PositiveInfinity;
                item.PreviousVertex = null;
                item.PreviousEdge = null;
                item.VertexCount = 0;
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

                edge.SourceVertex.Neighbours.Add(edge.TargetVertex);
                var key = GetKey(edge.SourceVertex.Id, edge.TargetVertex.Id);
                if (!_edges.ContainsKey(key))
                {
                    _edges.Add(key, edge);
                }
                else if (_edges[key].Cost > edge.Cost)
                {
                    _edges[key] = edge;
                }
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
                edge.SourceVertex.Neighbours.Add(edge.TargetVertex);
                var key = GetKey(edge.SourceVertex.Id, edge.TargetVertex.Id);
                if (!_edges.ContainsKey(key))
                {
                    _edges.Add(key, edge);
                }
                else if (_edges[key].Cost > edge.Cost)
                {
                    _edges[key] = edge;
                }
            }
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
    }
}