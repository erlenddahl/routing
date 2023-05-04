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
