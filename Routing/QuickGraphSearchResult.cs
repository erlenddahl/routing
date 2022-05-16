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

        public QuickGraphSearchResult(DijkstraResult dr)
        {
            Source = dr.Source;
            Target = dr.Target;

            var vertex = dr.Target;
            Items = new int[vertex.VertexCount];
            var ix = Items.Length - 1;
            while (vertex?.PreviousEdge != null)
            {
                Items[ix--] = vertex.PreviousEdge.DataItem.EdgeId;
                vertex = vertex.PreviousVertex;
            }
        }

        public VertexData Source { get; }
        public VertexData Target { get; }
    }
}