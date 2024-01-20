using System.Collections.Generic;

namespace Routing
{
    public class VertexDataComparer<T> : IComparer<VertexData<T>>
    {
        public int Compare(VertexData<T> x, VertexData<T> y)
        {
            var comp = x.Cost.CompareTo(y.Cost);
            if (comp == 0) return x.Vertex.Id.CompareTo(y.Vertex.Id);
            return comp;
        }
    }
    public class HVertexDataComparer<T> : IComparer<VertexData<T>>
    {
        public int Compare(VertexData<T> x, VertexData<T> y)
        {
            var comp = (x.Cost + x.H).CompareTo(y.Cost + y.H);
            if (comp == 0) return x.Vertex.Id.CompareTo(y.Vertex.Id);
            return comp;
        }
    }
}