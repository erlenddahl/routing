namespace Routing.Tests
{
    public static class TestGraphGenerator
    {
        public static Graph<GraphDataItem> StraightLine()
        {
            return Graph<GraphDataItem>.Create(new[]
            {
                new GraphDataItem()
                {
                    Cost = 1,
                    ReverseCost = double.MaxValue,
                    EdgeId = 0,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },
                new GraphDataItem()
                {
                    Cost = 1,
                    ReverseCost = double.MaxValue,
                    EdgeId = 1,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    Cost = 1,
                    ReverseCost = double.MaxValue,
                    EdgeId = 2,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                }
            });
        }
    }
}