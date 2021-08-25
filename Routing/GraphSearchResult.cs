using System;
using System.Collections.Generic;

namespace Routing
{
    public class GraphSearchResult : IEnumerable<GraphDataItem>
    {
        private readonly List<GraphDataItem> _items = new List<GraphDataItem>();

        public GraphSearchResult(Vertex vertex, TimeSpan time, int tries)
        {
            Vertex = vertex;
            while (vertex?.PreviousEdge != null)
            {
                var dataItem = vertex.PreviousEdge.DataItem.Clone();
                dataItem.IsReverse = vertex.PreviousEdge.IsReverse;
                _items.Add(dataItem);
                vertex = vertex.PreviousVertex;
            }

            _items.Reverse();

            for (var i = 0; i < _items.Count; i++)
                _items[i].Num = i + 1;

            PathFound = vertex != null;
            TimeSpent = time;
            Tries = tries;
        }

        public Vertex Vertex { get; private set; }
        public bool PathFound { get; private set; }
        public TimeSpan TimeSpent { get; private set; }
        public int Tries { get; private set; }

        public IEnumerator<GraphDataItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}