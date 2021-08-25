using System.Collections.Generic;
using System.Diagnostics;

namespace Routing
{
    public class QuickGraphSearchResult
    {
        public int[] Items { get; set; }

        public QuickGraphSearchResult()
        {

        }

        public QuickGraphSearchResult(Vertex source, Vertex target)
        {
            Source = source;
            Target = target;

            var vertex = target;
            Items = new int[vertex.VertexCount];
            var ix = Items.Length - 1;
            while (vertex?.PreviousEdge != null)
            {
                Items[ix--] = vertex.PreviousEdge.DataItem.EdgeId;
                vertex = vertex.PreviousVertex;
            }
        }

        public Vertex Source { get; }
        public Vertex Target { get; }
    }
}