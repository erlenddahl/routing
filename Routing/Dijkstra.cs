using System.Collections.Generic;

namespace Routing
{
    public class Dijkstra
    {
        public static (Vertex Target, int Tries) GetShortestPath(Graph graph, int sourceVertexId, int targetVertexId)
        {
            var current = graph.Vertices[sourceVertexId];
            current.Cost = 0;
            var queue = new SortedSet<Vertex>(new VertexComparer()) {current};

            var tries = 0;
            while (queue.Count > 0)
            {
                current = queue.Min;
                queue.Remove(current);

                tries++;
                if (current.Id == targetVertexId)
                {
                    return (current, tries);
                }

                foreach (var n in current.Neighbours)
                {
                    if (n.Visited) continue;

                    var edge = graph.GetEdge(current.Id, n.Id);

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

            return (null, tries);
        }
    }
}