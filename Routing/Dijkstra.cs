using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Routing
{
    public class DijkstraResult
    {
        private readonly Graph _graph;
        private readonly Dictionary<int, VertexData> _dynamicData;
        private readonly Stopwatch _stopwatch;

        public VertexData Source { get; set; }
        public VertexData Target { get; private set; }
        public TimeSpan ElapsedTime { get; set; }

        public int Tries { get; set; }

        public DijkstraResult(Graph graph)
        {
            _graph = graph; 
            _dynamicData = new Dictionary<int, VertexData>();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public VertexData GetVertexData(int id)
        {
            if (_dynamicData.TryGetValue(id, out var vd)) return vd;
            vd = new VertexData(_graph.Vertices[id]);
            _dynamicData.Add(id, vd);
            return vd;
        }

        public DijkstraResult Finish(VertexData target = null)
        {
            _stopwatch.Stop();
            ElapsedTime = _stopwatch.Elapsed;
            Target = target;
            return this;
        }

        public bool HasVisitedVertex(int id)
        {
            return _dynamicData.ContainsKey(id);
        }
    }

    public class Dijkstra
    {
        public static DijkstraResult GetShortestPath(Graph graph, int sourceVertexId, int targetVertexId)
        {
            var result = new DijkstraResult(graph);

            var current = result.GetVertexData(sourceVertexId);
            current.Cost = 0;
            result.Source = current;

            var queue = new SortedSet<VertexData>(new VertexDataComparer()) {current};

            while (queue.Count > 0)
            {
                current = queue.Min;
                queue.Remove(current);

                result.Tries++;
                if (current.Vertex.Id == targetVertexId)
                {
                    return result.Finish(current);
                }

                foreach (var n in current.Vertex.Neighbours.Select(p => result.GetVertexData(p.Id)))
                {
                    if (n.Visited) continue;

                    var edge = graph.GetEdge(current.Vertex.Id, n.Vertex.Id);

                    var totalCost = current.Cost + edge.Cost;
                    if (totalCost < n.Cost)
                    {
                        queue.Remove(n); // Must remove before changing to avoid upsetting the sorting by changing the sort value
                        n.Cost = totalCost;
                        n.VertexCount = current.VertexCount + 1;
                        n.PreviousVertex = current;
                        n.PreviousEdge = edge;
                        queue.Add(n);
                    }
                }

                //mark Vertex visited
                current.Visited = true;
            }

            return result.Finish();
        }
    }
}