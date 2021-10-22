namespace RoadNetworkRouting
{
    public class NetworkNode
    {
        public double X;
        public double Y;
        public int Id;
        public int Edges;
        public int VertexGroup { get; set; } = -1;

        public NetworkNode(double x, double y, int id)
        {
            X = x;
            Y = y;
            Id = id;
            Edges = 1;
        }
    }
}