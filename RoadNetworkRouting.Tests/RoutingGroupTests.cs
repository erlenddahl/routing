using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Exceptions;

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

            try
            {
                router.Search(new Point3D(-41025, 6868128), new Point3D(-32489, 6859998));
            }
            catch (DifferentGroupsException ex)
            {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void FromWesternIslandToEasternIslandFails()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            try
            {
                router.Search(new Point3D(-41025, 6868128), new Point3D(-23826, 6857736));
            }
            catch (DifferentGroupsException ex)
            {
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void HasThreeGroups()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            Assert.IsTrue(router.Links.All(p => p.Value.NetworkGroup >= 0));
            Assert.AreEqual(3, router.Links.GroupBy(p => p.Value.NetworkGroup).Count());
        }

        [TestMethod]
        public void FromWesternIslandToMiddleIslandWorksIfAllowed()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");

            router.Search(new Point3D(-41025, 6868128), new Point3D(-32489, 6859998), new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });
        }

        [TestMethod]
        public void FromWesternIslandToEasternIslandWorksIfAllowed()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            
            router.Search(new Point3D(-41025, 6868128), new Point3D(-23826, 6857736), new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });
        }
    }
}