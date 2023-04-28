namespace RoadNetworkRouting.Network
{
    public class Node
    {
        public double X;
        public double Y;
        public int Id;
        public int Edges;
        public int VertexGroup { get; set; } = -1;

        public Node(double x, double y, int id)
        {
            X = x;
            Y = y;
            Id = id;
            Edges = 1;
        }
    }
}