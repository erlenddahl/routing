using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Tests
{
    [TestClass]
    public class SaveAndLoadSkeletonTests
    {
        [TestMethod]
        public void WriteSkeletonNetwork()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveSkeletonTo("test_write.bin", new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });
        }

        [TestMethod]
        public void VerifyWrittenSkeletonNetwork()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveSkeletonTo("test_write.bin", new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });

            router = RoadNetworkRouter.LoadFrom(@"test_write.bin", skeletonConfig: new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });
            Assert.IsNotNull(router);
            Assert.AreEqual(332, router.Links.Count);
            Assert.AreEqual(331, router.Vertices.Count);
        }

        [TestMethod]
        public void LoadingSkeletonWithoutConfigFails()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveSkeletonTo("test_write.bin", new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });

            try
            {
                router = RoadNetworkRouter.LoadFrom(@"test_write.bin");
            }
            catch (MissingConfigException mcex)
            {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void SkeletonNetworkHasNoGeometryAtFirst()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveSkeletonTo("test_write.bin", new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });

            router = RoadNetworkRouter.LoadFrom(@"test_write.bin", skeletonConfig: new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });
            Assert.IsTrue(router.Links.Values.All(p => p.Geometry == null));
        }

        [TestMethod]
        public void SkeletonNetworkLoadsGeometryWhenNeeded()
        {
            var router = RoadNetworkRouter.LoadFrom(@"..\..\..\..\Data\network_three_islands.bin");
            router.SaveSkeletonTo("test_write.bin", new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });
            router = RoadNetworkRouter.LoadFrom(@"test_write.bin", skeletonConfig: new SkeletonConfig() { LinkDataDirectory = "test_linkdata_dir" });

            var linksWithGeometryBeforeSearch = router.Links.Values.Count(p => p.Geometry != null);
            var res = router.Search(new Point3D(-41025, 6868128), new Point3D(-38475, 6868576));

            var linksWithGeometryAfterSearch = router.Links.Values.Count(p => p.Geometry != null);

            Assert.AreEqual(0, linksWithGeometryBeforeSearch);
            Assert.AreEqual(200, linksWithGeometryAfterSearch);
            Assert.IsTrue(res.links.All(p => p.Geometry != null));
        }
    }
}