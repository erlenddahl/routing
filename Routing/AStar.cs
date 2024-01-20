using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing
{
    public class AStar
    {
        public static DijkstraResult<T> GetShortestPath<T>(Graph<T> graph, int sourceVertexId, int targetVertexId, Func<Vertex, Vertex, double> heuristic, GraphOverloader<T> overloader = null, double maxCost = double.MaxValue, double maxSearchDurationMs = double.MaxValue, long maxIterations = long.MaxValue)
        {
            var result = new DijkstraResult<T>(graph, overloader);

            var current = result.GetVertexData(sourceVertexId);
            current.Cost = 0;
            result.Source = current;

            var targetVertex = result.GetVertexData(targetVertexId).Vertex;

            var queue = new PriorityQueue<VertexData<T>>(new HVertexDataComparer<T>());
            queue.Add(current);

            VertexData<T> minHeuristic = null;
            var sw = new Stopwatch();
            sw.Start();

            while (queue.Count > 0)
            {
                if (sw.ElapsedMilliseconds > maxSearchDurationMs)
                {
                    return result.Finish(minHeuristic, TerminationType.TimedOut);
                }
                if (result.Iterations > maxIterations)
                {
                    return result.Finish(minHeuristic, TerminationType.TooManyIterations);
                }

                current = queue.Remove();

                result.Iterations++;
                if (current.Vertex.Id == targetVertexId)
                {
                    return result.Finish(current, TerminationType.ReachedTarget);
                }

                foreach (var n in current.Vertex.NeighbourIds.Select(p => result.GetVertexData(p)))
                {
                    if (n.Visited) continue;

                    var edge = result.GetEdge(current.Vertex.Id, n.Vertex.Id);

                    var cost = current.Cost + edge.Cost;

                    if (cost > maxCost)
                    {
                        result.AboveMaxCost++;
                        continue;
                    }

                    var hScore = heuristic(n.Vertex, targetVertex);
                    var fScore = cost + hScore;

                    if (fScore < n.Cost)
                    {
                        n.Cost = cost;
                        n.Heuristic = hScore;
                        n.VertexCount = current.VertexCount + 1;
                        n.PreviousVertex = current;
                        n.PreviousEdge = edge;
                        queue.Add(n); // Add to queue with updated fScore

                        if (minHeuristic == null || hScore < minHeuristic.Heuristic)
                            minHeuristic = n;
                    }
                }

                //mark Vertex visited
                current.Visited = true;
            }

            return result.Finish();
        }
    }
}