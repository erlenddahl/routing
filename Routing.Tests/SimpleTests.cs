using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Routing.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void StraightLine()
        {
            var graph = TestGraphGenerator.StraightLine();
            var path = graph.GetShortestPath(0, 3);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(3, path.Target.Vertex.Id);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, path.Items);
        }


        [TestMethod]
        public void ForkedLine_PicksTheUpper_Cheaper()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/   \___
            //      \___/ 
            //
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Upper section (uphill, flat, downhill)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },

                // Lower section (downhill, flat, uphill)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 5
                },
                new GraphDataItem()
                {
                    EdgeId = 1004,
                    SourceVertexId = 5,
                    TargetVertexId = 6,
                    Cost = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1006,
                    SourceVertexId = 6,
                    TargetVertexId = 4
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 4,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1001, 1003, 1005, 1007 }, path.Items);
        }


        [TestMethod]
        public void ForkedLine_PicksTheLower_Cheaper()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/   \___
            //      \___/ 
            //
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Upper section (uphill, flat, downhill)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 2,
                    TargetVertexId = 3,
                    Cost = 19
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },

                // Lower section (downhill, flat, uphill)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 5,
                    Cost = 7
                },
                new GraphDataItem()
                {
                    EdgeId = 1004,
                    SourceVertexId = 5,
                    TargetVertexId = 6,
                    Cost = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1006,
                    SourceVertexId = 6,
                    TargetVertexId = 4
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 4,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1002, 1004, 1006, 1007 }, path.Items);
        }


        [TestMethod]
        public void ForkedLine_PicksTheStraightPart()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/___\___
            //     
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Upper section (uphill, flat, downhill)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },

                // Lower section (flat)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 4
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 4,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1002, 1007 }, path.Items);
        }


        [TestMethod]
        public void ForkedLine_PicksTheDetourIfStraightIsExpensive()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/___\___
            //     
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Upper section (uphill, flat, downhill)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },

                // Lower section (flat)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 4,
                    Cost = 4
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 4,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1001, 1003, 1005, 1007 }, path.Items);
        }


        [TestMethod]
        public void ForkedLine_PicksTheDetourIfItIsNegative()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/___\___
            //     
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Upper section (uphill, flat, downhill)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 2,
                    TargetVertexId = 3,
                    Cost = -4
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },

                // Lower section (flat)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 4,
                    Cost = 1
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 4,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1001, 1003, 1005, 1007 }, path.Items);
        }


        [TestMethod]
        public void AvoidsLoop_NormalCosts()
        {
            // Create a road network that looks something like this:
            //       ___
            //   ___/___\___
            //     
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Lower section (flat)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },

                // Upper section (loop)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 3,
                    TargetVertexId = 4
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 4,
                    TargetVertexId = 2
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 2,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1002, 1007 }, path.Items);
        }


        [TestMethod]
        public void AvoidsLoop_NegativeCosts()
        {
            // Create a road network that looks something like this:
            //      __
            //   ___\/___
            //     
            var links = new[]
            {
                // First flat section
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },

                // Lower section (flat)
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },

                // Upper section (loop)
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 2,
                    TargetVertexId = 3,
                    Cost = -1
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 3,
                    TargetVertexId = 4,
                    Cost = -1
                },
                new GraphDataItem()
                {
                    EdgeId = 1005,
                    SourceVertexId = 4,
                    TargetVertexId = 2,
                    Cost = -1
                },

                // Final flat section
                new GraphDataItem()
                {
                    EdgeId = 1007,
                    SourceVertexId = 2,
                    TargetVertexId = 7
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 7);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(7, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1002, 1007 }, path.Items);
        }

        [TestMethod]
        public void StaysOnStraightLine()
        {
            var links = new[]
            {
                new GraphDataItem()
                {
                    EdgeId = 1000,
                    SourceVertexId = 0,
                    TargetVertexId = 1
                },
                new GraphDataItem()
                {
                    EdgeId = 1001,
                    SourceVertexId = 1,
                    TargetVertexId = 2
                },
                new GraphDataItem()
                {
                    EdgeId = 1002,
                    SourceVertexId = 2,
                    TargetVertexId = 3
                },
                new GraphDataItem()
                {
                    EdgeId = 1003,
                    SourceVertexId = 1,
                    TargetVertexId = 4
                }
            };

            var graph = Graph.Create(links);
            var path = graph.GetShortestPath(0, 3);

            Assert.AreEqual(0, path.Source.Vertex.Id);
            Assert.AreEqual(3, path.Target.Vertex.Id);

            Debug.WriteLine("Picked route: " + string.Join(", ", path.Items));
            CollectionAssert.AreEqual(new[] { 1000, 1001, 1002 }, path.Items);
        }
    }
}
