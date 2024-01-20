using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing
{
    public class AStar
    {
        public static DijkstraResult<T> GetShortestPath<T>(Graph<T> graph, int sourceVertexId, int targetVertexId, Func<Vertex, Vertex, double> heuristic, GraphOverloader<T> overloader = null, double maxCost = double.MaxValue)
        {
            var result = new DijkstraResult<T>(graph, overloader);

            var current = result.GetVertexData(sourceVertexId);
            current.Cost = 0;
            result.Source = current;

            var targetVertex = result.GetVertexData(targetVertexId).Vertex;

            var queue = new PriorityQueue<VertexData<T>>(new HVertexDataComparer<T>());
            queue.Add(current);

            while (queue.Count > 0)
            {
                current = queue.Remove();

                result.Tries++;
                if (current.Vertex.Id == targetVertexId)
                {
                    return result.Finish(current);
                }

                foreach (var n in current.Vertex.NeighbourIds.Select(p => result.GetVertexData(p)))
                {
                    if (n.Visited) continue;

                    var edge = result.GetEdge(current.Vertex.Id, n.Vertex.Id);

                    var cost = current.Cost + edge.Cost;

                    if (cost > maxCost) continue;

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
                    }
                }

                //mark Vertex visited
                current.Visited = true;
            }

            return result.Finish();
        }
    }
}