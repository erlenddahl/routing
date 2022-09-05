using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Routing.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void SourceOverloading()
        {
            var graph = TestGraphGenerator.StraightLine();
            var path = graph.GetShortestPath(0, 3);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(3, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }
    }
}
