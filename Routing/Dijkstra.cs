using System;
using System.Linq;

namespace Routing
{
    public class Dijkstra
    {

        public static DijkstraResult<T> GetShortestPath<T>(Graph<T> graph, int sourceVertexId, int targetVertexId, GraphOverloader<T> overloader = null, double maxCost = double.MaxValue)
        {
            var result = new DijkstraResult<T>(graph, overloader);

            var current = result.GetVertexData(sourceVertexId);
            current.Cost = 0;
            result.Source = current;

            var queue = new PriorityQueue<VertexData<T>>(new VertexDataComparer<T>());
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

                    var totalCost = current.Cost + edge.Cost;

                    if (totalCost > maxCost)
                    {
                        result.AboveMaxCost++;
                        continue;
                    }

                    if (totalCost < n.Cost)
                    {
                        //queue.Remove(n); // Must remove before changing to avoid upsetting the sorting by changing the sort value
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