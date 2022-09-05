using System.Collections.Generic;
using System.Linq;

namespace Routing
{
    public class Dijkstra
    {
        public static DijkstraResult GetShortestPath(Graph graph, int sourceVertexId, int targetVertexId, GraphOverloader overloader = null)
        {
            var result = new DijkstraResult(graph, overloader);

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

                foreach (var n in current.Vertex.NeighbourIds.Select(p => result.GetVertexData(p)))
                {
                    if (n.Visited) continue;

                    var edge = result.GetEdge(current.Vertex.Id, n.Vertex.Id);

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