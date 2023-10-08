using System;
using System.Collections.Generic;

namespace Routing
{
    public class Vertex : IEqualityComparer<Vertex>
    {
        public int Id { get; set; }
        public HashSet<int> NeighbourIds { get; set; } = new();

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

        public Vertex Clone()
        {
            return new Vertex()
            {
                Id = Id,
                NeighbourIds = new HashSet<int>(NeighbourIds)
            };
        }
    }
}