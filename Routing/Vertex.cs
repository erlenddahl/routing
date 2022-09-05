using System.Collections.Generic;

namespace Routing
{
    public class VertexData
    {
        public Vertex Vertex { get; }
        public bool Visited { get; set; }
        public double Cost { get; set; } = double.PositiveInfinity;
        public VertexData PreviousVertex { get; set; }
        public Edge PreviousEdge { get; set; }
        /// <summary>
        /// The total number of vertices from the start to this point in the search.
        /// </summary>
        public int VertexCount { get; set; }

        public VertexData(Vertex vertex)
        {
            Vertex = vertex;
        }

        public override string ToString()
        {
            return $"{Vertex.Id} (cost={Cost:n2})";
        }
    }

    public class Vertex : IEqualityComparer<Vertex>
    {
        public int Id { get; set; }
        public HashSet<int> NeighbourIds { get; set; } = new HashSet<int>();

        public override string ToString()
        {
            return Id.ToString();
        }

        public bool Equals(Vertex x, Vertex y)
        {
            return x == y;
        }

        public int GetHashCode(Vertex obj)
        {
            return IntegerHash(obj.Id);
        }

        static int IntegerHash(int a)
        {
            // fmix32 from murmurhash
            uint h = (uint)a;
            h ^= h >> 16;
            h *= 0x85ebca6bU;
            h ^= h >> 13;
            h *= 0xc2b2ae35U;
            h ^= h >> 16;
            return (int)h;
        }
    }
}