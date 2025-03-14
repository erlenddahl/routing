using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoadNetworkRouting;

namespace RoutingApi.Tests
{
    [TestClass]
    public class GeoJsonLoadTest
    {
        [TestMethod]
        public void LoadTest()
        {
            // Hard coded GeoJSON path for simple testing
            var router = RoadNetworkRouter.BuildFromGeoJson(@"G:\2022-06-30 - Veglenker Trondheim.geojson");
            Assert.AreEqual(17690, router.Links.Count, 100);
            Assert.AreEqual(14817, router.GenerateVertices().Count, 200);
        }
    }
}