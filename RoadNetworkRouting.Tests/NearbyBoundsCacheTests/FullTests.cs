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
    public class FullTests
    {
        [TestMethod]
        public void MultipleItemsDirectHit()
        {
            var bounds = new[]
            {
                new BoundingBox2D(55, 65, 51, 98),
                new BoundingBox2D(67, 75, 65, 69),
                new BoundingBox2D(53, 55, 55, 85)
            };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Hits the first box
            Assert.AreEqual(1, cache.GetNearbyItems(56, 58).Count());

            // Hits the second box
            Assert.AreEqual(1, cache.GetNearbyItems(68, 68).Count());

            // Hits the third box
            Assert.AreEqual(1, cache.GetNearbyItems(54, 68).Count());
        }

        [TestMethod]
        public void MultipleItemsSmallRadius()
        {
            var bounds = new[]
            {
                new BoundingBox2D(55, 65, 51, 98),
                new BoundingBox2D(67, 75, 65, 69),
                new BoundingBox2D(53, 55, 55, 85)
            };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Hits the first box
            Assert.AreEqual(2, cache.GetNearbyItems(56, 58, 10).Count());

            // Hits the second box
            Assert.AreEqual(2, cache.GetNearbyItems(68, 68, 10).Count());

            // Hits the third box
            Assert.AreEqual(2, cache.GetNearbyItems(54, 68, 10).Count());
        }

        [TestMethod]
        public void MultipleItemsLargeRadius()
        {
            var bounds = new[]
            {
                new BoundingBox2D(55, 65, 51, 98),
                new BoundingBox2D(67, 75, 65, 69),
                new BoundingBox2D(53, 55, 55, 85)
            };
            var cache = NearbyBoundsCache<BoundingBox2D>.FromBounds(bounds, p => p, 50);

            // Hits the first box
            Assert.AreEqual(3, cache.GetNearbyItems(56, 58, 100).Count());

            // Hits the second box
            Assert.AreEqual(3, cache.GetNearbyItems(68, 68, 100).Count());

            // Hits the third box
            Assert.AreEqual(3, cache.GetNearbyItems(54, 68, 100).Count());
        }
    }
}
