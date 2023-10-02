using System.Collections.Generic;
using System.Diagnostics;

namespace Routing
{
    public class QuickGraphSearchResult
    {
        public VertexData Source { get; }
        public VertexData Target { get; }
        public int[] Edges { get; set; }
        public int[] Vertices { get; set; }

        public DijkstraResult InternalData { get; set; }

        public QuickGraphSearchResult(VertexData source, VertexData target)
        {
            Source = source;
            Target = target;
        }

        public QuickGraphSearchResult(DijkstraResult dr)
        {
            Source = dr.Source;
            Target = dr.Target;

            InternalData = dr;

            if (Target == null) return;

            var vertex = dr.Target;
            Edges = new int[vertex.VertexCount];
            var ix = Edges.Length - 1;
            while (vertex?.PreviousEdge != null)
            {
                Edges[ix--] = vertex.PreviousEdge.DataItem.EdgeId;
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