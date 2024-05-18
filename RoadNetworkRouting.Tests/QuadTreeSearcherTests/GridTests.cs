using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Tests.QuadTreeSearcherTests
{
    [TestClass]
    public class GridTests
    {

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeBounds_Nearby()
        {
            // This item is strictly within the -50--100, -50--100 cell.
            var bounds = new[] { new BoundingBox2D(-65, -55, -65, -55) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.Find(-5, -5).Count());
            Assert.AreEqual(0, cache.Find(-55, -5).Count());
            Assert.AreEqual(0, cache.Find(-105, -5).Count());
            Assert.AreEqual(0, cache.Find(-5, -55).Count());
            Assert.AreEqual(0, cache.Find(-105, -55).Count());
            Assert.AreEqual(0, cache.Find(-5, -105).Count());
            Assert.AreEqual(0, cache.Find(-55, -105).Count());
            Assert.AreEqual(0, cache.Find(-105, -105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.Find(-55, -55).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeX_Nearby()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(-105, -37, 640, 719) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            Assert.AreEqual(1, cache.Find(-42, 688).Count());
        }

        [TestMethod]
        public void SingleItemWithinSingleCell_NegativeY_Nearby()
        {
            // This item overlaps with four cells.
            var bounds = new[] { new BoundingBox2D(911, 976, -972, -888) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            for (var x = 801; x <= 1101; x += 50)
            for (var y = -1099; y <= -799; y += 50)
            {
                var shouldContain = bounds[0].Contains(x, y);
                Assert.AreEqual(shouldContain ? 1 : 0, cache.Find(x, y).Count());
            }
        }

        [TestMethod]
        public void SingleItemThatAlmostFillsSingleCell()
        {
            // This item is strictly within the 50-100, 50-100 cell.
            var bounds = new[] { new BoundingBox2D(51, 99, 51, 99) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.Find(5, 5).Count());
            Assert.AreEqual(0, cache.Find(55, 5).Count());
            Assert.AreEqual(0, cache.Find(105, 5).Count());
            Assert.AreEqual(0, cache.Find(5, 55).Count());
            Assert.AreEqual(0, cache.Find(105, 55).Count());
            Assert.AreEqual(0, cache.Find(5, 105).Count());
            Assert.AreEqual(0, cache.Find(55, 105).Count());
            Assert.AreEqual(0, cache.Find(105, 105).Count());

            // Check that there is a single item in the middle cell
            Assert.AreEqual(1, cache.Find(55, 55).Count());
        }

        [TestMethod]
        public void MultipleItemsWithinSingleCell()
        {
            var bounds = new[]
            {
                new BoundingBox2D(55, 65, 51, 98),
                new BoundingBox2D(67, 75, 65, 69),
                new BoundingBox2D(53, 55, 55, 85)
            };
            var cache = BoundsQuadTreeItem.Create(bounds);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.Find(5, 5).Count());
            Assert.AreEqual(0, cache.Find(55, 5).Count());
            Assert.AreEqual(0, cache.Find(105, 5).Count());
            Assert.AreEqual(0, cache.Find(5, 55).Count());
            Assert.AreEqual(0, cache.Find(105, 55).Count());
            Assert.AreEqual(0, cache.Find(5, 105).Count());
            Assert.AreEqual(0, cache.Find(55, 105).Count());
            Assert.AreEqual(0, cache.Find(105, 105).Count());

            // Hits two of the items
            Assert.AreEqual(2, cache.Find(55, 55).Count());

            // Hits the third
            Assert.AreEqual(1, cache.Find(68, 66).Count());
        }

        [TestMethod]
        public void SingleItemOverlappingTwoCells()
        {
            // This item spans two cells; 50_50 and 100_50
            var bounds = new[] { new BoundingBox2D(58, 137, 60, 70) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.Find(5, 5).Count());
            Assert.AreEqual(0, cache.Find(55, 5).Count());
            Assert.AreEqual(0, cache.Find(105, 5).Count());
            Assert.AreEqual(0, cache.Find(155, 5).Count());
            Assert.AreEqual(0, cache.Find(5, 55).Count());
            Assert.AreEqual(0, cache.Find(155, 55).Count());
            Assert.AreEqual(0, cache.Find(5, 105).Count());
            Assert.AreEqual(0, cache.Find(55, 105).Count());
            Assert.AreEqual(0, cache.Find(105, 105).Count());
            Assert.AreEqual(0, cache.Find(155, 105).Count());

            // Check that there is a single item in the two middle cells
            Assert.AreEqual(1, cache.Find(59, 60).Count());
            Assert.AreEqual(1, cache.Find(105, 70).Count());

            // But only if actually inside the item area
            Assert.AreEqual(0, cache.Find(55, 65).Count());
            Assert.AreEqual(0, cache.Find(105, 55).Count());
        }

        [TestMethod]
        public void SingleItemOverlappingTwoCellsAroundOrigin()
        {
            // This item spans two cells; -50_50 and 50_50
            var bounds = new[] { new BoundingBox2D(-38, 37, 60, 70) };
            var cache = BoundsQuadTreeItem.Create(bounds);

            // Check that there are no items in the cells surrounding this item.
            Assert.AreEqual(0, cache.Find(-95, 5).Count());
            Assert.AreEqual(0, cache.Find(-45, 5).Count());
            Assert.AreEqual(0, cache.Find(5, 5).Count());
            Assert.AreEqual(0, cache.Find(55, 5).Count());
            Assert.AreEqual(0, cache.Find(105, 5).Count());
                                                   
            Assert.AreEqual(0, cache.Find(-95, 55).Count());
            Assert.AreEqual(0, cache.Find(55, 55).Count());
            Assert.AreEqual(0, cache.Find(105, 55).Count());
                                                   
            Assert.AreEqual(0, cache.Find(-95,105).Count());
            Assert.AreEqual(0, cache.Find(-45, 105).Count());
            Assert.AreEqual(0, cache.Find(5, 105).Count());
            Assert.AreEqual(0, cache.Find(55, 105).Count());
            Assert.AreEqual(0, cache.Find(105, 105).Count());

            // Check that there is a single item in the two center cells
            Assert.AreEqual(1, cache.Find(-35, 65).Count());
            Assert.AreEqual(1, cache.Find(5, 65).Count());

            // But only if actually inside the item area
            Assert.AreEqual(0, cache.Find(-45, 55).Count());
            Assert.AreEqual(0, cache.Find(5, 55).Count());
        }
    }
}
