using System.Collections.Generic;

namespace Routing
{
    public class VertexDataComparer : IComparer<VertexData>
    {
        public int Compare(VertexData x, VertexData y)
        {
            var comp = x.Cost.CompareTo(y.Cost);
            if (comp == 0) return x.Vertex.Id.CompareTo(y.Vertex.Id);
            return comp;
        }
    }
}