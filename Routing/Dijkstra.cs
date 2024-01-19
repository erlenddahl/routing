using System;
using System.Collections.Generic;
using System.Linq;

namespace Routing
{
    public class Dijkstra
    {
        /// <summary>
        /// Priority queue class that's slightly quicker than SortedSet because we don't need to
        /// remove and re-add an item after updating its cost (we simply add it again). This may
        /// cause duplicates in the queue, but that's no biggie -- they will be skipped later anyway.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class PriorityQueue<T> 
        {
            private readonly IComparer<T> _comparer;
            private readonly List<T> _data = new();

            public PriorityQueue(IComparer<T> comparer)
            {
                _comparer = comparer;
            }

            public void Add(T item)
            {
                _data.Add(item);
                var ci = _data.Count - 1; // child index; start at end
                while (ci > 0)
                {
                    var pi = (ci - 1) / 2; // parent index
                    if (_comparer.Compare(_data[ci], _data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
                    (_data[ci], _data[pi]) = (_data[pi], _data[ci]);
                    ci = pi;
                }
            }

            public T Remove()
            {
                // assumes pq is not empty; up to calling code
                var li = _data.Count - 1; // last index (before removal)
                var frontItem = _data[0];   // fetch the front
                _data[0] = _data[li];
                _data.RemoveAt(li);

                --li; // last index (after removal)
                var pi = 0; // parent index. start at front of pq
                while (true)
                {
                    var ci = pi * 2 + 1; // left child index of parent
                    if (ci > li) break;  // no children so done
                    var rc = ci + 1;     // right child
                    if (rc <= li && _comparer.Compare(_data[rc], _data[ci]) < 0) ci = rc; // if rc exists and is smaller than lc, use rc instead
                    if (_comparer.Compare(_data[pi], _data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                    (_data[pi], _data[ci]) = (_data[ci], _data[pi]);
                    pi = ci;
                }
                return frontItem;
            }

            public int Count => _data.Count;
        }

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

                    if (totalCost > maxCost) continue;

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