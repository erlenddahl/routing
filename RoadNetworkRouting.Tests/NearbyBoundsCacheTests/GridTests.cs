using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Utils;

namespace RoadNetworkRouting.Tests.NearbyBoundsCacheTests
{
    [TestClass]
    public class GridTests
    {
        [TestMethod]
        public void SingleItemWithinSingleCell()
        {
            // This item is strictly within the 50-100, 50-100 cell.
            var bounds = new[] { new BoundingBox2D(55, 65, 55, 65) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.GetItemsInCell(55, 55).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeBounds()
        {
            // This item is strictly within the -50--100, -50--100 cell.
            var bounds = new[] { new BoundingBox2D(-65, -55, -65, -55) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(-5, -5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-55, -5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-105, -5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-5, -55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-105, -55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-5, -105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-55, -105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-105, -105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.GetItemsInCell(-55, -55).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeBounds_Nearby()
        {
            // This item is strictly within the -50--100, -50--100 cell.
            var bounds = new[] { new BoundingBox2D(-65, -55, -65, -55) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetNearbyItems(-5, -5).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-55, -5).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-105, -5).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-5, -55).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-105, -55).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-5, -105).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-55, -105).Count());
            Assert.AreEqual(0, cache.GetNearbyItems(-105, -105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.GetNearbyItems(-55, -55).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeX()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(-105, -37, 640, 719) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            Assert.AreEqual(1, cache.GetItemsInCell(-42, 688).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeX_Nearby()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(-105, -37, 640, 719) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            Assert.AreEqual(1, cache.GetNearbyItems(-42, 688).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeY()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(911, 976, -972, -888) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            for (var x = 801; x <= 1101; x += 50)
            for (var y = -1099; y <= -799; y += 50)
            {
                var shouldContain = (x == 901 || x == 951) && (y == -999 || y == -949 || y == -899);
                Assert.AreEqual(shouldContain ? 1 : 0, cache.GetItemsInCell(x, y).Count());
            }
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeY_Nearby()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(911, 976, -972, -888) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            for (var x = 801; x <= 1101; x += 50)
            for (var y = -1099; y <= -799; y += 50)
            {
                var shouldContain = bounds[0].Contains(x, y);
                Assert.AreEqual(shouldContain ? 1 : 0, cache.GetNearbyItems(x, y).Count());
            }
        }

        [TestMethod]
        public void SingleItemThatAlmostFillsSingleCell()
        {
            // This item is strictly within the 50-100, 50-100 cell.
            var bounds = new[] { new BoundingBox2D(51, 99, 51, 99) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.GetItemsInCell(55, 55).Count());
        }

        [TestMethod]
        public void SingleItemThatFillsSingleCell()
        {
            // This item completely filles the 50_50 cell. Since it is on the border to all the neighbouring cells, it should be included there as well.
            var bounds = new[] { new BoundingBox2D(50, 100, 50, 100) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there is a single item in all cells surrounding this cell
            Assert.AreEqual(1, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(105, 5).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(5, 55).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(55, 55).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(105, 55).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(105, 105).Count());
        }

        [TestMethod]
        public void MultipleItemsWithinSingleCell()
        {
            // This item is strictly within the 50-100, 50-100 cell.
            var bounds = new[]
            {
                new BoundingBox2D(55, 65, 51, 98),
                new BoundingBox2D(67, 75, 65, 69),
                new BoundingBox2D(53, 55, 55, 85)
            };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(3, cache.GetItemsInCell(55, 55).Count());
        }

        [TestMethod]
        public void SingleItemOverlappingTwoCells()
        {
            // This item spans two cells; 50_50 and 100_50
            var bounds = new[] { new BoundingBox2D(58, 137, 60, 70) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(155, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(155, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(155, 105).Count());

            // Check that there is a single item in the two middle cells
            Assert.AreEqual(1, cache.GetItemsInCell(55, 55).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(105, 55).Count());
        }

        [TestMethod]
        public void SingleItemOverlappingTwoCellsAroundOrigin()
        {
            // This item spans two cells; -50_50 and 50_50
            var bounds = new[] { new BoundingBox2D(-38, 37, 60, 70) };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.GetItemsInCell(-95, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-45, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 5).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 5).Count());

            Assert.AreEqual(0, cache.GetItemsInCell(-95, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 55).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 55).Count());

            Assert.AreEqual(0, cache.GetItemsInCell(-95,105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(-45, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(5, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(55, 105).Count());
            Assert.AreEqual(0, cache.GetItemsInCell(105, 105).Count());

            // Check that there is a single item in the two middle cells
            Assert.AreEqual(1, cache.GetItemsInCell(-45, 55).Count());
            Assert.AreEqual(1, cache.GetItemsInCell(5, 55).Count());
        }
    }
}
