namespace Routing;

public class VertexData<T>
{
    public Vertex Vertex { get; }
    public bool Visited { get; set; }
    public double Cost { get; set; } = double.PositiveInfinity;
    public double Heuristic { get; set; } = double.PositiveInfinity;
    public VertexData<T> PreviousVertex { get; set; }
    public Edge<T> PreviousEdge { get; set; }
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