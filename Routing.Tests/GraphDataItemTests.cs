using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Routing.Tests;

[TestClass]
public class GraphDataItemTests
{
    [TestMethod]
    public void ToBytesAndBack()
    {
        var originalItem = new GraphDataItem
        {
            EdgeId = 1,
            SourceVertexId = 2,
            TargetVertexId = 3,
            Cost = 4.0,
            ReverseCost = 5.0
        };

        var bytes = originalItem.ToBytes();
        var restoredItem = GraphDataItem.FromBytes(bytes);

        Assert.AreEqual(originalItem.EdgeId, restoredItem.EdgeId);
        Assert.AreEqual(originalItem.SourceVertexId, restoredItem.SourceVertexId);
        Assert.AreEqual(originalItem.TargetVertexId, restoredItem.TargetVertexId);
        Assert.AreEqual(originalItem.Cost, restoredItem.Cost);
        Assert.AreEqual(originalItem.ReverseCost, restoredItem.ReverseCost);
    }
}