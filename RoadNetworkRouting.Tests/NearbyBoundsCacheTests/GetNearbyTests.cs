using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoadNetworkRouting.Utils;

namespace RoadNetworkRouting.Tests.NearbyBoundsCacheTests
{
    [TestClass]
    public class GetNearbyTests : NearbyBoundsCache<int>
    {
        public GetNearbyTests() : base(50)
        {
        }

        [TestMethod]
        public void Complex()
        {
            var items = new List<GridColumn>()
            {
                new(-45000, 5000, new List<GridCell>() { new(0, null) }),
                new(-40000, 5000, new List<GridCell>() { new(1, null) }),
                new(-35000, 5000, new List<GridCell>() { new(2, null) }),
                new(-30000, 5000, new List<GridCell>() { new(3, null) }),
                new(-25000, 5000, new List<GridCell>() { new(4, null) }),
                new(-20000, 5000, new List<GridCell>() { new(5, null) }),
                new(-15000, 5000, new List<GridCell>() { new(6, null) }),
                new(-10000, 5000, new List<GridCell>() { new(7, null) }),
                new(-5000, 5000, new List<GridCell>() { new(8, null) }),
                new(0, 5000, new List<GridCell>() { new(9, null) }),
                new(30000, 5000, new List<GridCell>() { new(10, null) }),
                new(35000, 5000, new List<GridCell>() { new(11, null) }),
                new(65000, 5000, new List<GridCell>() { new(12, null) }),
                new(70000, 5000, new List<GridCell>() { new(13, null) }),
                new(75000, 5000, new List<GridCell>() { new(14, null) }),
                new(80000, 5000, new List<GridCell>() { new(15, null) }),
                new(85000, 5000, new List<GridCell>() { new(16, null) }),
                new(95000, 5000, new List<GridCell>() { new(17, null) }),
                new(100000, 5000, new List<GridCell>() { new(18, null) }),
                new(105000, 5000, new List<GridCell>() { new(19, null) }),
                new(110000, 5000, new List<GridCell>() { new(20, null) }),
                new(115000, 5000, new List<GridCell>() { new(21, null) }),
                new(120000, 5000, new List<GridCell>() { new(22, null) }),
                new(125000, 5000, new List<GridCell>() { new(23, null) }),
                new(130000, 5000, new List<GridCell>() { new(24, null) }),
                new(135000, 5000, new List<GridCell>() { new(25, null) }),
                new(140000, 5000, new List<GridCell>() { new(26, null) }),
                new(145000, 5000, new List<GridCell>() { new(27, null) }),
                new(150000, 5000, new List<GridCell>() { new(28, null) }),
                new(155000, 5000, new List<GridCell>() { new(29, null) }),
                new(160000, 5000, new List<GridCell>() { new(30, null) }),
                new(165000, 5000, new List<GridCell>() { new(31, null) }),
                new(170000, 5000, new List<GridCell>() { new(32, null) }),
                new(175000, 5000, new List<GridCell>() { new(33, null) }),
                new(180000, 5000, new List<GridCell>() { new(34, null) }),
                new(185000, 5000, new List<GridCell>() { new(35, null) }),
                new(190000, 5000, new List<GridCell>() { new(36, null) }),
                new(195000, 5000, new List<GridCell>() { new(37, null) }),
                new(200000, 5000, new List<GridCell>() { new(38, null) }),
                new(205000, 5000, new List<GridCell>() { new(39, null) }),
                new(210000, 5000, new List<GridCell>() { new(40, null) }),
                new(215000, 5000, new List<GridCell>() { new(41, null) }),
                new(220000, 5000, new List<GridCell>() { new(42, null) }),
                new(225000, 5000, new List<GridCell>() { new(43, null) }),
                new(230000, 5000, new List<GridCell>() { new(44, null) }),
                new(235000, 5000, new List<GridCell>() { new(45, null) }),
                new(260000, 5000, new List<GridCell>() { new(46, null) }),
                new(265000, 5000, new List<GridCell>() { new(47, null) }),
                new(270000, 5000, new List<GridCell>() { new(48, null) }),
                new(275000, 5000, new List<GridCell>() { new(49, null) }),
                new(280000, 5000, new List<GridCell>() { new(50, null) }),
                new(285000, 5000, new List<GridCell>() { new(51, null) }),
                new(290000, 5000, new List<GridCell>() { new(52, null) }),
                new(295000, 5000, new List<GridCell>() { new(53, null) }),
                new(300000, 5000, new List<GridCell>() { new(54, null) }),
                new(305000, 5000, new List<GridCell>() { new(55, null) }),
                new(310000, 5000, new List<GridCell>() { new(56, null) }),
                new(315000, 5000, new List<GridCell>() { new(57, null) })
            };
            
            var nearby = GetNearby(items, -41699.806, 100).ToArray();
            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(0, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 41699.806, 100).ToArray();
            Assert.AreEqual(0, nearby.Length);

            nearby = GetNearby(items, 191581.881, 100).ToArray();
            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(36, nearby[0].Items[0].Item);
        }

        [TestMethod]
        public void SimplePositive()
        {
            var items = new List<GridColumn>()
            {
                new(0, 50, new List<GridCell>(){new(1, null)}),
                new(50, 50, new List<GridCell>(){new(2, null)}),
                new(100, 50, new List<GridCell>(){new(3, null)})
            };
            var nearby = GetNearby(items, 1, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(1, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 67, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(2, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 120, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(3, nearby[0].Items[0].Item);
        }

        [TestMethod]
        public void SimpleNegative()
        {
            var items = new List<GridColumn>()
            {
                new(-150, 50, new List<GridCell>(){new(1, null)}),
                new(-100, 50, new List<GridCell>(){new(2, null)}),
                new(-50, 50, new List<GridCell>(){new(3, null)})
            };
            var nearby = GetNearby(items, -1, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(3, nearby[0].Items[0].Item);

            nearby = GetNearby(items, -67, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(2, nearby[0].Items[0].Item);

            nearby = GetNearby(items, -120, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(1, nearby[0].Items[0].Item);
        }

        [TestMethod]
        public void SimpleMixed()
        {
            var items = new List<GridColumn>()
            {
                new(-150, 50, new List<GridCell>(){new(1, null)}),
                new(-100, 50, new List<GridCell>(){new(2, null)}),
                new(-50, 50, new List<GridCell>(){new(3, null)}),
                new(0, 50, new List<GridCell>(){new(4, null)}),
                new(50, 50, new List<GridCell>(){new(5, null)}),
                new(100, 50, new List<GridCell>(){new(6, null)})
            };
            var nearby = GetNearby(items, -1, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(3, nearby[0].Items[0].Item);

            nearby = GetNearby(items, -67, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(2, nearby[0].Items[0].Item);

            nearby = GetNearby(items, -120, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(1, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 1, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(4, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 67, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(5, nearby[0].Items[0].Item);

            nearby = GetNearby(items, 120, 0).ToArray();

            Assert.AreEqual(1, nearby.Length);
            Assert.AreEqual(1, nearby[0].Items.Count);
            Assert.AreEqual(6, nearby[0].Items[0].Item);
        }
    }
}
