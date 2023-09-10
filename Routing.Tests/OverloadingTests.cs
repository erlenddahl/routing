using System;
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

            Assert.AreEqual(2.3, path.InternalData.Target.Cost, 0.1);

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

            Assert.AreEqual(2.3, path.InternalData.Target.Cost, 0.1);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(-1, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }

        [TestMethod]
        public void SourceAndTargetOverloading()
        {
            var graph = TestGraphGenerator.StraightLine();
            var overloader = new GraphOverloader();
            overloader.AddSourceOverload(-1, 0, 1, 0.3);
            overloader.AddTargetOverload(-2, 2, 3, 0.4);

            var path = graph.GetShortestPath(-1, -2, overloader);

            Assert.AreEqual(1.7, path.InternalData.Target.Cost, 0.1);

            Assert.AreEqual(-1, path.Source.Vertex.Id);
            Assert.AreEqual(-2, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }
        [TestMethod]
        public void SourceOverloading_NegativeFactorTHrows()
        {
            var graph = TestGraphGenerator.StraightLine();
            var overloader = new GraphOverloader();
            try
            {
                overloader.AddSourceOverload(-1, 0, 1, -0.3);
            }
            catch (Exception ex)
            {
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void TargetOverloading_TooLargeFactorThrows()
        {
            var graph = TestGraphGenerator.StraightLine();
            var overloader = new GraphOverloader();
            try
            {
                overloader.AddTargetOverload(-1, 0, 1, 1.3);
            }
            catch (Exception ex)
            {
                return;
            }
            Assert.Fail();
        }
    }
}
