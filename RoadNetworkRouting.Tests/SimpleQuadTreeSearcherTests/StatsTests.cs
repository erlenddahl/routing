using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoadNetworkRouting.Utils;

namespace RoadNetworkRouting.Tests.SimpleQuadTreeSearcherTests
{
    [TestClass]
    public class StatsTests
    {
        [TestMethod]
        public void FromRoot_CountsZoomsAndBoundaryChecks()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);
        }


        [TestMethod]
        public void RepeatedFromFoundCell_NoMoreZoomsOneCheckReturn()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);


            (cell, items) = cache.FindCell(cell, 500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(5, stats.BoundaryChecks);
            Assert.AreEqual(2, stats.LeavesReturned);
        }


        [TestMethod]
        public void MultipleRepeatsFromFoundCell_NoMoreZoomsOneCheckReturn()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);


            for (var i = 500; i < 525; i++)
            {
                (cell, items) = cache.FindCell(cell, 500, 500, stats);
                Assert.AreEqual(500, cell.Bounds.Xmin, 25);
                Assert.AreEqual(500, cell.Bounds.Ymin, 25);
                Assert.AreEqual(3, cell.Depth);
            }

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(29, stats.BoundaryChecks);
            Assert.AreEqual(26, stats.LeavesReturned);
        }


        [TestMethod]
        public void SlightlyOutsideOfReturnedCell_ZoomsOutAndInAgain()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);


            (cell, items) = cache.FindCell(cell, 499, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(5, stats.ZoomedIn);
            Assert.AreEqual(2, stats.ZoomedOut);
            Assert.AreEqual(9, stats.BoundaryChecks);
            Assert.AreEqual(2, stats.LeavesReturned);
        }


        [TestMethod]
        public void FarAway_ZoomsOutToRootAndInAgain()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);


            (cell, items) = cache.FindCell(cell, 1500, 500, stats);
            Assert.AreEqual(1500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(250, cell.Bounds.Ymin, 25);
            Assert.AreEqual(1, cell.Depth);

            Assert.AreEqual(4, stats.ZoomedIn);
            Assert.AreEqual(3, stats.ZoomedOut);
            Assert.AreEqual(9, stats.BoundaryChecks);
            Assert.AreEqual(2, stats.LeavesReturned);
        }


        [TestMethod]
        public void FarOutsideOfCoveredArea()
        {
            var stats = new TreeNavigationStatistics();
            var (bounds, _) = FullTests.GenerateTestSet();

            var cache = BoundsQuadTreeItem.Create(bounds);

            var (cell, items) = cache.FindCell(500, 500, stats);
            Assert.AreEqual(500, cell.Bounds.Xmin, 25);
            Assert.AreEqual(500, cell.Bounds.Ymin, 25);
            Assert.AreEqual(3, cell.Depth);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(0, stats.ZoomedOut);
            Assert.AreEqual(4, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);
            Assert.AreEqual(0, stats.CompletelyOutside);


            (cell, items) = cache.FindCell(cell, 150_000, 500_000, stats);
            Assert.IsNull(cell);

            Assert.AreEqual(3, stats.ZoomedIn);
            Assert.AreEqual(4, stats.ZoomedOut);
            Assert.AreEqual(8, stats.BoundaryChecks);
            Assert.AreEqual(1, stats.LeavesReturned);
            Assert.AreEqual(1, stats.CompletelyOutside);
        }
    }
}
