using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Tests
{
    /// <summary>
    /// Tests routing on a road network consisting of three isolated islands. Useful for testing searches that goes outside of the main network in Norway.
    /// </summary>
    [TestClass]
    public class RoutingGroupTests
    {
        [TestMethod]
        public void InternalOnWesternIslandWorks()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            var res = router.Search(new Point3D(-41025, 6868128), new Point3D(-38475, 6868576));

            Assert.IsTrue(res.Success);
            Assert.IsTrue(res.Links.Length > 0);
        }

        [TestMethod]
        public void InternalOnMiddleIslandWorks()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            var res = router.Search(new Point3D(-34282, 6862473), new Point3D(-32489, 6859998));

            Assert.IsTrue(res.Success);
            Assert.IsTrue(res.Links.Length > 0);
        }

        [TestMethod]
        public void InternalOnEasternIslandWorks()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            var res = router.Search(new Point3D(-27667, 6858206), new Point3D(-23826, 6857736));

            Assert.IsTrue(res.Success);
            Assert.IsTrue(res.Links.Length > 0);
        }

        [TestMethod]
        public void FromWesternIslandToMiddleIslandFails()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            var res = router.Search(new Point3D(-41025, 6868128), new Point3D(-32489, 6859998));

            Assert.IsFalse(res.Success);
        }

        [TestMethod]
        public void FromWesternIslandToEasternIslandFails()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            var res = router.Search(new Point3D(-41025, 6868128), new Point3D(-23826, 6857736));

            Assert.IsFalse(res.Success);
        }
    }
}