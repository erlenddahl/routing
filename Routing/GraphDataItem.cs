namespace Routing
{
    public class GraphDataItem
    {
        public string Id { get; set; }
        public int EdgeId { get; set; }
        public int SourceVertexId { get; set; }
        public int TargetVertexId { get; set; }
        public double Cost { get; set; }
        public double ReverseCost { get; set; }
        public bool IsReverse { get; set; }
        public int Num { get; set; }

        public GraphDataItem Clone()
        {
            return new GraphDataItem
            {
                Id=Id,
                EdgeId = EdgeId,
                SourceVertexId = SourceVertexId,
                TargetVertexId = TargetVertexId,
                Cost = Cost,
                ReverseCost = ReverseCost,
            };
        }
    }
}