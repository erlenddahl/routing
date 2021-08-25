using System.Collections.Generic;

namespace Routing
{
    public class VertexComparer : IComparer<Vertex>
    {
        public int Compare(Vertex x, Vertex y)
        {
            var comp = x.Cost.CompareTo(y.Cost);
            if (comp == 0) return x.Id.CompareTo(y.Id);
            return comp;
        }
    }
}