using System;

namespace Routing
{
    public class GraphDataItem
    {
        public int EdgeId { get; set; }
        public int SourceVertexId { get; set; }
        public int TargetVertexId { get; set; }
        public double Cost { get; set; } = 1;
        public double ReverseCost { get; set; }

        public byte[] ToBytes()
        {
            var totalSize = sizeof(int) * 3 + sizeof(double) * 2;
            var result = new byte[totalSize];
            Span<byte> span = result;

            BitConverter.TryWriteBytes(span.Slice(0, sizeof(int)), EdgeId);
            BitConverter.TryWriteBytes(span.Slice(4, sizeof(int)), SourceVertexId);
            BitConverter.TryWriteBytes(span.Slice(8, sizeof(int)), TargetVertexId);
            BitConverter.TryWriteBytes(span.Slice(12, sizeof(double)), Cost);
            BitConverter.TryWriteBytes(span.Slice(20, sizeof(double)), ReverseCost);

            return result;
        }

        public static GraphDataItem FromBytes(Span<byte> bytes)
        {
            return new GraphDataItem
            {
                EdgeId = BitConverter.ToInt32(bytes.Slice(0, 4)),
                SourceVertexId = BitConverter.ToInt32(bytes.Slice(4, 4)),
                TargetVertexId = BitConverter.ToInt32(bytes.Slice(8, 4)),
                Cost = BitConverter.ToDouble(bytes.Slice(12, 8)),
                ReverseCost = BitConverter.ToDouble(bytes.Slice(20, 8))
            };
        }

    }
}