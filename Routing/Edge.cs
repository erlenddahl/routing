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

        /// <summary>
        /// Is set to true if this is an edge created due to an overloaded source or target node.
        /// </summary>
        public bool IsOverload { get; set; }

        public Edge Clone()
        {
            return new Edge()
            {
                Id = Id,
                SourceVertex = SourceVertex,
                TargetVertex = TargetVertex,
                Cost = Cost,
                IsReverse = IsReverse,
                DataItem = DataItem
            };
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}