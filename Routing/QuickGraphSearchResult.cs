using System.Collections.Generic;
using System.Diagnostics;

namespace Routing
{
    public class QuickGraphSearchResult<T>
    {
        public VertexData<T> Source { get; }
        public VertexData<T> Target { get; }
        public T[] Edges { get; set; }
        public int[] Vertices { get; set; }

        public DijkstraResult<T> InternalData { get; set; }

        public QuickGraphSearchResult(VertexData<T> source, VertexData<T> target)
        {
            Source = source;
            Target = target;
        }

        public QuickGraphSearchResult(DijkstraResult<T> dr)
        {
            Source = dr.Source;
            Target = dr.Target;

            InternalData = dr;

            if (Target == null) return;

            var vertex = dr.Target;
            Edges = new T[vertex.VertexCount];
            var ix = Edges.Length - 1;
            while (vertex?.PreviousEdge != null)
            {
                Edges[ix--] = vertex.PreviousEdge.DataItem;
                vertex = vertex.PreviousVertex;
            }

            vertex = dr.Target;
            Vertices = new int[vertex.VertexCount + 1];
            ix = Vertices.Length - 2;
            Vertices[^1] = vertex.Vertex.Id;
            while (vertex?.PreviousVertex != null)
            {
                Vertices[ix--] = vertex.PreviousVertex.Vertex.Id;
                vertex = vertex.PreviousVertex;
            }
        }
    }
}