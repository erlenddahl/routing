using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Config;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class FindNearestLinkTests
{
    private RoadNetworkRouter _router;

    [TestInitialize]
    public void Init()
    {
        var networkFile = @"..\\..\\..\\..\\Data\\network_troendelag.bin";
        _router = RoadNetworkRouter.LoadFrom(networkFile);
    }

    [TestMethod]
    public void PointAtNode()
    {
        var res = _router.GetNearestLink(new Point3D(261079.2, 7041288.2), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(3596, res.Nearest.Distance, 1);

        Assert.AreEqual(res.Nearest.X, res.Link.Geometry[^1].X);
        Assert.AreEqual(res.Nearest.Y, res.Link.Geometry[^1].Y);
    }

    [TestMethod]
    public void PointInsideLink()
    {
        var res = _router.GetNearestLink(new Point3D(261807.7, 7041127.1), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(2680, res.Nearest.Distance, 1);
    }

    [TestMethod]
    public void PointAtOtherSide()
    {
        var res = _router.GetNearestLink(new Point3D(263800.5783, 7040584.6060), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(0, res.Nearest.Distance);

        Assert.AreEqual(res.Nearest.X, res.Link.Geometry[0].X);
        Assert.AreEqual(res.Nearest.Y, res.Link.Geometry[0].Y);
    }

    [TestMethod]
    public void AnotherLink()
    {
        var res = _router.GetNearestLink(new Point3D(263801.1999, 7040584.5386), new RoutingConfig());
        Assert.AreEqual(335696, res.Link.LinkId);
        Assert.AreEqual(6, res.Link.Geometry.Length);

        Assert.AreEqual(res.Link.QueryPointInfo(double.MaxValue).Distance, res.Nearest.Distance);

        Assert.AreEqual(res.Nearest.X, res.Link.Geometry[^1].X);
        Assert.AreEqual(res.Nearest.Y, res.Link.Geometry[^1].Y);
    }

    [TestMethod]
    public void YetAnotherLink()
    {
        var res = _router.GetNearestLink(new Point3D(271804.17, 7037782.86), new RoutingConfig());
        Assert.AreEqual(608243, res.Link.LinkId);
    }
}