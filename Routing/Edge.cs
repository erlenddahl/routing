namespace Routing
{
    public class Edge
    {
        public int Id { get; set; }
        public Vertex SourceVertex { get; set; }
        public Vertex TargetVertex { get; set; }
        public double Cost { get; set; }
        public bool IsReverse { get; set; }
        public GraphDataItem DataItem { get; set; }
    }
}