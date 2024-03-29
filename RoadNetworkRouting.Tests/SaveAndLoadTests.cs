using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests
{
    [TestClass]
    public class SaveAndLoadTests
    {
        [TestMethod]
        public void WriteAndLoadTinyNetwork()
        {
            var router = RoadNetworkRouter.Build(new[]
            {
                new RoadLink() { LinkId = 0, FromNodeId = 0, ToNodeId = 1, Geometry = new[]{new Point3D(0,0), new Point3D(1,1)} }
            });
            Assert.IsNotNull(router);
            Assert.AreEqual(1, router.Links.Count);
            Assert.AreEqual(2, router.GenerateVertices().Count);

            router.SaveTo("test_write_and_load.bin");

            router = RoadNetworkRouter.LoadFrom("test_write_and_load.bin");
            Assert.IsNotNull(router);
            Assert.AreEqual(1, router.Links.Count);
            Assert.AreEqual(2, router.GenerateVertices().Count);
        }

        [TestMethod]
        public void LoadFullNetwork()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            Assert.IsNotNull(router);
            Assert.AreEqual(332, router.Links.Count);
            Assert.AreEqual(331, router.GenerateVertices().Count);
        }

        [TestMethod]
        public void WriteFullNetwork()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveTo("test_write.bin");
        }

        [TestMethod]
        public void VerifyWrittenNetwork()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveTo("test_write.bin");
            
            router = RoadNetworkRouter.LoadFrom(@"test_write.bin");
            Assert.IsNotNull(router);
            Assert.AreEqual(332, router.Links.Count);
            Assert.AreEqual(331, router.GenerateVertices().Count);
        }
    }
}