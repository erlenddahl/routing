using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Routing.Tests
{
    [TestClass]
    public class OverloadingTests
    {
        [TestMethod]
        public void SourceOverloading()
        {
            var graph = TestGraphGenerator.StraightLine();
            var overloader = new GraphOverloader();
            overloader.AddSourceOverload(-1, 0, 1, 0.3);

            var path = graph.GetShortestPath(-1, 3, overloader);

            Assert.AreEqual(-1, path.Source.Vertex.Id);
            Assert.AreEqual(3, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }

        [TestMethod]
        public void TargetOverloading()
        {
            var graph = TestGraphGenerator.StraightLine();
            var overloader = new GraphOverloader();
            overloader.AddTargetOverload(-1, 2, 3, 0.3);

            var path = graph.GetShortestPath(0, -1, overloader);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(-1, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }
    }
}
