namespace Routing
{
    public class GraphDataItem
    {
        public int EdgeId { get; set; }
        public int SourceVertexId { get; set; }
        public int TargetVertexId { get; set; }
        public double Cost { get; set; } = 1;
        public double ReverseCost { get; set; }
        public bool IsReverse { get; set; }
        public int Num { get; set; }

        public GraphDataItem Clone()
        {
            return new GraphDataItem
            {
                EdgeId = EdgeId,
                SourceVertexId = SourceVertexId,
                TargetVertexId = TargetVertexId,
                Cost = Cost,
                ReverseCost = ReverseCost,
            };
        }
    }
}