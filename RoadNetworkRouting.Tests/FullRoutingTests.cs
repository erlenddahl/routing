using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class RoutingTests_FullNetwork : RoutingTests
{
    [TestInitialize]
    public override void Init()
    {
        var networkFile = "D:\\Lager\\RouteNetworkUpdater\\2023-01-09\\network.bin";
        _router = RoadNetworkRouter.LoadFrom(networkFile);
    }
}

public abstract class RoutingTests
{
    protected RoadNetworkRouter _router;

    public abstract void Init();

    [TestMethod]
    public void GetNearestLink_PointAtNode()
    {
        var res = _router.GetNearestLink(new Point3D(261079.2, 7041288.2), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(3596, res.Nearest.Distance, 1);

        Assert.AreEqual(res.Nearest.X, res.Link.Geometry[^1].X);
        Assert.AreEqual(res.Nearest.Y, res.Link.Geometry[^1].Y);
    }

    [TestMethod]
    public void GetNearestLink_PointInsideLink()
    {
        var res = _router.GetNearestLink(new Point3D(261807.7, 7041127.1), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(2680, res.Nearest.Distance, 1);
    }

    [TestMethod]
    public void GetNearestLink_PointAtOtherSide()
    {
        var res = _router.GetNearestLink(new Point3D(263800.5783, 7040584.6060), new RoutingConfig());
        Assert.AreEqual(335707, res.Link.LinkId);
        Assert.AreEqual(175, res.Link.Geometry.Length);

        Assert.AreEqual(0, res.Nearest.Distance);

        Assert.AreEqual(res.Nearest.X, res.Link.Geometry[0].X);
        Assert.AreEqual(res.Nearest.Y, res.Link.Geometry[0].Y);
    }

    [TestMethod]
    public void SingleLink_FromStartToEnd()
    {
        var res = _router.Search(new Point3D(263800.5783, 7040584.6060), new Point3D(261079.2, 7041288.2), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(175, res.Links[0].Geometry.Length);
        Assert.AreEqual(2811, new Point3D(261079.2, 7041288.2).DistanceTo2D(new Point3D(263800.5783, 7040584.6060)), 1);
        Assert.AreEqual(3596, res.RouteDistance, 1);

        var start = _router.Links[335707].Geometry[0];
        var end = _router.Links[335707].Geometry[^1];
        Assert.AreEqual(start, res.Links[0].Geometry[0]);
        Assert.AreEqual(end, res.Links[0].Geometry[^1]);
    }

    [TestMethod]
    public void SingleLink_FromStartToMiddle()
    {
        var res = _router.Search(new Point3D(263800.5783, 7040584.6060), new Point3D(262443.0, 7040825.6), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(75, res.Links[0].Geometry.Length);

        Assert.AreEqual(1643, res.RouteDistance, 1);

        var start = _router.Links[335707].Geometry[0];
        var end = _router.Links[335707].Geometry[74];
        Assert.AreEqual(start, res.Links[0].Geometry[0]);
        Assert.AreEqual(end, res.Links[0].Geometry[^1]);
    }

    [TestMethod]
    public void SingleLink_FromEndToStart()
    {
        var res = _router.Search(new Point3D(261079.2, 7041288.2), new Point3D(263800.5783, 7040584.6060), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(175, res.Links[0].Geometry.Length);
        Assert.AreEqual(2811, new Point3D(261079.2, 7041288.2).DistanceTo2D(new Point3D(263800.5783, 7040584.6060)), 1);
        Assert.AreEqual(3596, res.RouteDistance, 1);

        var start = _router.Links[335707].Geometry[^1];
        var end = _router.Links[335707].Geometry[0];
        Assert.AreEqual(start, res.Links[0].Geometry[0]);
        Assert.AreEqual(end, res.Links[0].Geometry[^1]);
    }

    [TestMethod]
    public void SingleLink_FromEndToMiddle()
    {
        var res = _router.Search(new Point3D(261079.2, 7041288.2), new Point3D(262443.0, 7040825.6), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(101, res.Links[0].Geometry.Length);

        Assert.AreEqual(1953, res.RouteDistance, 1);

        var start = _router.Links[335707].Geometry[^1];
        var end = _router.Links[335707].Geometry[74];
        Assert.AreEqual(start, res.Links[0].Geometry[0]);
        Assert.AreEqual(end, res.Links[0].Geometry[^1]);
    }

    [TestMethod]
    public void SingleLink_BothDirections_FromOneThirdToTwoThirds()
    {
        var res = _router.Search(new Point3D(262725.5, 7040681.7), new Point3D(261848.6, 7041068.2), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(63, res.Links[0].Geometry.Length);

        Assert.AreEqual(1211, res.RouteDistance, 1);

        Assert.AreEqual(262711, res.Links[0].Geometry[0].X, 1);
        Assert.AreEqual(7040651, res.Links[0].Geometry[0].Y, 1);
        Assert.AreEqual(394, res.Links[0].Geometry[0].Z, 1);

        Assert.AreEqual(261800, res.Links[0].Geometry[^1].X, 1);
        Assert.AreEqual(7041032, res.Links[0].Geometry[^1].Y, 1);
        Assert.AreEqual(417, res.Links[0].Geometry[^1].Z, 1);
    }

    [TestMethod]
    public void SingleLink_BothDirections_FromTwoThirdsToOneThird()
    {
        var res = _router.Search(new Point3D(261848.6, 7041068.2), new Point3D(262725.5, 7040681.7), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);
        Assert.AreEqual(63, res.Links[0].Geometry.Length);

        Assert.AreEqual(1211, res.RouteDistance, 1);

        Assert.AreEqual(262711, res.Links[0].Geometry[^1].X, 1);
        Assert.AreEqual(7040651, res.Links[0].Geometry[^1].Y, 1);
        Assert.AreEqual(394, res.Links[0].Geometry[^1].Z, 1);

        Assert.AreEqual(261800, res.Links[0].Geometry[0].X, 1);
        Assert.AreEqual(7041032, res.Links[0].Geometry[0].Y, 1);
        Assert.AreEqual(417, res.Links[0].Geometry[0].Z, 1);
    }

    [TestMethod]
    public void SingleLink_OneWay_LegalDrivingDirection()
    {
        var res = _router.Search(new Point3D(270155.529, 7042095.454), new Point3D(270130.155, 7042079.387), new RoutingConfig());

        Assert.AreEqual(1, res.Links.Length);

        Assert.AreEqual(30, res.RouteDistance, 1);

        Assert.AreEqual(593586, res.Links[0].LinkId);
    }

    [TestMethod]
    public void SingleLink_OneWay_IllegalDrivingDirection_DriveAround()
    {
        var res = _router.Search(new Point3D(270130.155, 7042079.387), new Point3D(270155.529, 7042095.454), new RoutingConfig());

        Assert.AreEqual(11, res.Links.Length);

        Assert.AreEqual(415, res.RouteDistance, 1);

        Assert.AreEqual(593586, res.Links[0].LinkId);
        Assert.AreEqual(593594, res.Links[1].LinkId);
        Assert.AreEqual(473188, res.Links[2].LinkId);
        Assert.AreEqual(473180, res.Links[3].LinkId);
        Assert.AreEqual(1580985, res.Links[4].LinkId);
        Assert.AreEqual(1580977, res.Links[5].LinkId);
        Assert.AreEqual(564771, res.Links[6].LinkId);
        Assert.AreEqual(593562, res.Links[7].LinkId);
        Assert.AreEqual(593570, res.Links[8].LinkId);
        Assert.AreEqual(593578, res.Links[9].LinkId);
        Assert.AreEqual(593586, res.Links[10].LinkId);
    }

    [TestMethod]
    public void ThreeLinks_OneWay_LegalDrivingDirection()
    {
        var res = _router.Search(new Point3D(270179.57, 7042107.00), new Point3D(270130.59, 7042080.35), new RoutingConfig());

        Assert.AreEqual(3, res.Links.Length);

        Assert.AreEqual(56, res.RouteDistance, 1);

        Assert.AreEqual(593570, res.Links[0].LinkId);
        Assert.AreEqual(593578, res.Links[1].LinkId);
        Assert.AreEqual(593586, res.Links[2].LinkId);
    }

    [TestMethod]
    public void ThreeLinks_OneWay_IllegalDrivingDirection_DriveAround()
    {
        var res = _router.Search(new Point3D(270130.59, 7042080.35), new Point3D(270179.57, 7042107.00), new RoutingConfig());

        Assert.AreEqual(9, res.Links.Length);

        Assert.AreEqual(388, res.RouteDistance, 1);

        Assert.AreEqual(593586, res.Links[0].LinkId);
        Assert.AreEqual(593594, res.Links[1].LinkId);
        Assert.AreEqual(473188, res.Links[2].LinkId);
        Assert.AreEqual(473180, res.Links[3].LinkId);
        Assert.AreEqual(1580985, res.Links[4].LinkId);
        Assert.AreEqual(1580977, res.Links[5].LinkId);
        Assert.AreEqual(564771, res.Links[6].LinkId);
        Assert.AreEqual(593562, res.Links[7].LinkId);
        Assert.AreEqual(593570, res.Links[8].LinkId);
    }

    [TestMethod]
    public void TwoLinks_OneWay_LegalDrivingDirection()
    {
        var res = _router.Search(new Point3D(270165.015, 7042099.307), new Point3D(270130.59, 7042080.35), new RoutingConfig());

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(39, res.RouteDistance, 1);

        Assert.AreEqual(593578, res.Links[0].LinkId);
        Assert.AreEqual(593586, res.Links[1].LinkId);
    }

    [TestMethod]
    public void TwoLinks_OneWay_IllegalDrivingDirection_DriveAround()
    {
        var res = _router.Search(new Point3D(270130.59, 7042080.35), new Point3D(270165.015, 7042099.307), new RoutingConfig());

        Assert.AreEqual(10, res.Links.Length);

        Assert.AreEqual(404, res.RouteDistance, 1);

        Assert.AreEqual(593586, res.Links[0].LinkId);
        Assert.AreEqual(593594, res.Links[1].LinkId);
        Assert.AreEqual(473188, res.Links[2].LinkId);
        Assert.AreEqual(473180, res.Links[3].LinkId);
        Assert.AreEqual(1580985, res.Links[4].LinkId);
        Assert.AreEqual(1580977, res.Links[5].LinkId);
        Assert.AreEqual(564771, res.Links[6].LinkId);
        Assert.AreEqual(593562, res.Links[7].LinkId);
        Assert.AreEqual(593570, res.Links[8].LinkId);
        Assert.AreEqual(593578, res.Links[9].LinkId);
    }

    [TestMethod]
    public void FiveLinks_VariousGeometryDirections_EntireLinks()
    {
        var res = _router.Search(new Point3D(264310.48, 7040713.99), new Point3D(263801.32212, 7040584.45598), new RoutingConfig());

        Assert.AreEqual(5, res.Links.Length);

        Assert.AreEqual(487847, res.Links[0].LinkId);
        Assert.AreEqual(475764, res.Links[1].LinkId);
        Assert.AreEqual(1573429, res.Links[2].LinkId);
        Assert.AreEqual(1573437, res.Links[3].LinkId);
        Assert.AreEqual(335696, res.Links[4].LinkId);

        // No cutting -- all links should be whole.
        Assert.AreEqual(6, res.Links[0].Geometry.Length);
        Assert.AreEqual(8, res.Links[1].Geometry.Length);
        Assert.AreEqual(3, res.Links[2].Geometry.Length);
        Assert.AreEqual(6, res.Links[3].Geometry.Length);
        Assert.AreEqual(6, res.Links[4].Geometry.Length);

        // No cutting -- all links should be whole.
        Assert.AreEqual(87, res.Links[0].Length, 1);
        Assert.AreEqual(135, res.Links[1].Length, 1);
        Assert.AreEqual(116, res.Links[2].Length, 1);
        Assert.AreEqual(262, res.Links[3].Length, 1);
        Assert.AreEqual(80, res.Links[4].Length, 1);

        Assert.AreEqual(680, res.RouteDistance, 1);
    }

    [TestMethod]
    public void FiveLinks_VariousGeometryDirections_FirstAndLastCut()
    {
        var res = _router.Search(new Point3D(264306.57, 7040675.76), new Point3D(263836.97, 7040592.68), new RoutingConfig());

        Assert.AreEqual(5, res.Links.Length);

        Assert.AreEqual(487847, res.Links[0].LinkId);
        Assert.AreEqual(475764, res.Links[1].LinkId);
        Assert.AreEqual(1573429, res.Links[2].LinkId);
        Assert.AreEqual(1573437, res.Links[3].LinkId);
        Assert.AreEqual(335696, res.Links[4].LinkId);

        // First and last links are cut.
        Assert.AreEqual(6, res.Links[0].Geometry.Length);
        Assert.AreEqual(8, res.Links[1].Geometry.Length);
        Assert.AreEqual(3, res.Links[2].Geometry.Length);
        Assert.AreEqual(6, res.Links[3].Geometry.Length);
        Assert.AreEqual(5, res.Links[4].Geometry.Length);

        // First and last links are cut.
        Assert.AreEqual(57, res.Links[0].Length, 1);
        Assert.AreEqual(135, res.Links[1].Length, 1);
        Assert.AreEqual(116, res.Links[2].Length, 1);
        Assert.AreEqual(262, res.Links[3].Length, 1);
        Assert.AreEqual(43, res.Links[4].Length, 1);

        Assert.AreEqual(612, res.RouteDistance, 1);
    }
}

[TestClass]
public class RoutingTests_TinyNetwork : RoutingTests
{
    [TestInitialize]
    public override void Init()
    {
        _router = new RoadNetworkRouter(new RoadLink[]
        {
            new()
            {
                RoadClass = 7,
                LinkId = 487847,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 475955,
                ToNodeId = 490272,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 30,
                SpeedLimitReversed = 30,
                RoadType = "",
                Geometry = new[] { new Point3D(264296.9, 7040621.6, 357.345), new Point3D(264294.3, 7040626.6, 357.345), new Point3D(264294.1, 7040632.3, 357.545), new Point3D(264304, 7040653.8, 357.445), new Point3D(264306.1, 7040673.2, 358.145), new Point3D(264310.4, 7040705.3, 362.045) },
                LaneCode = "",
                Cost = 0.25551238656044006,
                ReverseCost = 0.25551238656044006
            },
            new()
            {
                RoadClass = 7,
                LinkId = 475764,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 475955,
                ToNodeId = 475956,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 30,
                SpeedLimitReversed = 30,
                RoadType = "",
                Geometry = new[] { new Point3D(264296.9, 7040621.6, 357.345), new Point3D(264291.6, 7040602.8, 357.345), new Point3D(264285.5, 7040589.2, 357.045), new Point3D(264259.9, 7040564.7, 355.045), new Point3D(264253.4, 7040554.3, 354.945), new Point3D(264244, 7040535.1, 355.545), new Point3D(264236.5, 7040512, 357.645), new Point3D(264236, 7040505.2, 357.945) },
                LaneCode = "",
                Cost = 0.3957824409008026,
                ReverseCost = 0.3957824409008026
            },
            new()
            {
                RoadClass = 6,
                LinkId = 1573429,
                FromRelativeLength = 0.7913469076156616,
                ToRelativeLength = 0.8131144046783447,
                FromNodeId = 475956,
                ToNodeId = 487377,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 60,
                SpeedLimitReversed = 60,
                RoadType = "",
                Geometry = new[] { new Point3D(264236, 7040505.2, 357.945), new Point3D(264201.7, 7040509.8, 360.745), new Point3D(264124.7, 7040534.2, 367.945) },
                LaneCode = "",
                Cost = 0.16845566034317017,
                ReverseCost = 0.16845566034317017
            },
            new()
            {
                RoadClass = 6,
                LinkId = 1573437,
                FromRelativeLength = 0.8131144046783447,
                ToRelativeLength = 0.8622568845748901,
                FromNodeId = 487377,
                ToNodeId = 326254,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 60,
                SpeedLimitReversed = 60,
                RoadType = "",
                Geometry = new[] { new Point3D(264124.7, 7040534.2, 367.945), new Point3D(264044.7, 7040560.8, 375.645), new Point3D(264008, 7040571.1, 379.245), new Point3D(263947.6, 7040594.3, 385.145), new Point3D(263920.9, 7040599.6, 387.546), new Point3D(263875.05, 7040604.05, 392.196) },
                LaneCode = "",
                Cost = 0.38020312786102295,
                ReverseCost = 0.38020312786102295
            },
            new()
            {
                RoadClass = 7,
                LinkId = 335696,
                FromRelativeLength = 0,
                ToRelativeLength = 0.02185666374862194,
                FromNodeId = 326254,
                ToNodeId = 326255,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 50,
                SpeedLimitReversed = 50,
                RoadType = "",
                Geometry = new[] { new Point3D(263875.05, 7040604.05, 392.196), new Point3D(263873.76, 7040599.37, 391.526), new Point3D(263870.15, 7040596.32, 391.286), new Point3D(263846.56, 7040594.42, 390.536), new Point3D(263817.2, 7040584.47, 390.226), new Point3D(263801.335, 7040584.454, 391.4079221) },
                LaneCode = "",
                Cost = 0.1413177102804184,
                ReverseCost = 0.1413177102804184
            },
            new()
            {
                FromNodeId = 0,
                ToNodeId = 1,
                LinkId = 335707,
                Cost = 5,
                ReverseCost = 5,
                Geometry = new Point3D[]
                {
                    new(263800.364, 7040584.665, 391.5134935),
                    new(263762.14, 7040594.92, 391.696),
                    new(263750.67, 7040594.81, 391.676),
                    new(263732.64, 7040587.44, 391.806),
                    new(263711.08, 7040571.13, 392.646),
                    new(263644.5, 7040549.33, 393.826),
                    new(263617.88, 7040538.03, 393.576),
                    new(263610.03, 7040535.75, 393.946),
                    new(263601.51, 7040535.9, 394.366),
                    new(263594.77, 7040538.74, 394.526),
                    new(263573.64, 7040554.43, 397.056),
                    new(263538.7, 7040572.4, 398.077),
                    new(263519.73, 7040578.52, 398.247),
                    new(263494.31, 7040582.4, 399.577),
                    new(263481.11, 7040586.26, 400.667),
                    new(263460.76, 7040593.96, 401.677),
                    new(263449.39, 7040599.87, 403.267),
                    new(263423.54, 7040616.9, 404.777),
                    new(263409.39, 7040626.3, 402.107),
                    new(263382.98, 7040639.08, 398.767),
                    new(263377.35, 7040644.97, 397.847),
                    new(263372.97, 7040659.22, 397.017),
                    new(263370.96, 7040661.49, 396.937),
                    new(263365.09, 7040663.04, 396.897),
                    new(263358.37, 7040661.02, 396.767),
                    new(263343.91, 7040651.48, 395.517),
                    new(263331.83, 7040641.4, 394.717),
                    new(263312.15, 7040623.87, 390.677),
                    new(263304.09, 7040614.12, 388.497),
                    new(263298.73, 7040600.08, 388.267),
                    new(263290.82, 7040589.47, 388.227),
                    new(263279.42, 7040580.45, 387.667),
                    new(263263.99, 7040571.12, 385.847),
                    new(263258.18, 7040564.58, 385.297),
                    new(263246.29, 7040543.37, 386.587),
                    new(263237.76, 7040527.75, 389.677),
                    new(263226.28, 7040510.86, 391.607),
                    new(263218.33, 7040488.31, 393.457),
                    new(263213.89, 7040481.3, 393.927),
                    new(263199.56, 7040467.25, 393.657),
                    new(263189.82, 7040444.06, 395.737),
                    new(263184.65, 7040435.33, 397.137),
                    new(263174.77, 7040427.21, 397.367),
                    new(263153.13, 7040415.57, 400.427),
                    new(263116.95, 7040418.64, 398.787),
                    new(263087.69, 7040416.76, 399.678),
                    new(263056.26, 7040414.6, 402.858),
                    new(263022.43, 7040415.87, 403.738),
                    new(262997.92, 7040421.73, 406.148),
                    new(262974.43, 7040427.12, 405.948),
                    new(262939.22, 7040435.51, 402.727),
                    new(262914.95, 7040438.61, 400.967),
                    new(262866.4, 7040440.96, 399.857),
                    new(262837.77, 7040446.25, 396.707),
                    new(262827.06, 7040450.54, 394.267),
                    new(262813.93, 7040466.21, 390.217),
                    new(262808.65, 7040480.46, 387.847),
                    new(262805.26, 7040515.7, 384.037),
                    new(262798.71, 7040533.47, 382.297),
                    new(262770.68, 7040574.94, 384.697),
                    new(262742.83, 7040620.85, 390.257),
                    new(262732, 7040636.03, 392.487),
                    new(262721.95, 7040645.93, 394.227),
                    new(262704.89, 7040654.12, 394.247),
                    new(262685.87, 7040656.49, 393.597),
                    new(262675.48, 7040655.65, 393.856),
                    new(262644, 7040650.26, 394.736),
                    new(262609.25, 7040642.53, 398.656),
                    new(262592.44, 7040640.28, 399.906),
                    new(262583.4, 7040642.06, 400.546),
                    new(262553.67, 7040653.79, 404.506),
                    new(262531.84, 7040659.99, 408.076),
                    new(262503.88, 7040663.21, 409.546),
                    new(262489.43, 7040663.2, 409.266),
                    new(262474.6, 7040660.65, 410.566),
                    new(262454.87, 7040654.07, 411.725),
                    new(262431.84, 7040642.81, 411.665),
                    new(262396.53, 7040623.67, 413.735),
                    new(262388.89, 7040620.6, 413.295),
                    new(262376.86, 7040619.86, 413.005),
                    new(262369.19, 7040621.8, 412.155),
                    new(262340.69, 7040633.18, 412.505),
                    new(262323.25, 7040646.03, 414.285),
                    new(262309.25, 7040663.05, 415.205),
                    new(262300.64, 7040677.6, 416.075),
                    new(262295.52, 7040689.45, 416.625),
                    new(262290.32, 7040711.07, 415.605),
                    new(262288.49, 7040714.76, 415.185),
                    new(262281.81, 7040718.96, 414.555),
                    new(262275.02, 7040719.46, 413.525),
                    new(262261.8, 7040715.67, 411.315),
                    new(262256.25, 7040712.91, 410.075),
                    new(262244.77, 7040702.05, 407.044),
                    new(262238.67, 7040698.29, 405.854),
                    new(262232.74, 7040697.23, 405.314),
                    new(262224.44, 7040699.21, 404.994),
                    new(262220.61, 7040701.75, 404.554),
                    new(262209.08, 7040713.86, 406.094),
                    new(262200.06, 7040718.55, 406.694),
                    new(262161.49, 7040722.57, 403.774),
                    new(262109.43, 7040729.19, 403.144),
                    new(262102.76, 7040729.48, 402.014),
                    new(262069.02, 7040723.91, 399.974),
                    new(262043.8, 7040720.05, 402.074),
                    new(262029.62, 7040716.24, 404.273),
                    new(262012.33, 7040708.8, 405.323),
                    new(261990.72, 7040698.11, 403.073),
                    new(261983.1, 7040698.52, 402.223),
                    new(261978.8, 7040701.08, 401.903),
                    new(261969.22, 7040717.79, 400.613),
                    new(261959.61, 7040746.31, 401.163),
                    new(261957.44, 7040760.44, 401.363),
                    new(261957.96, 7040780.09, 400.573),
                    new(261956.73, 7040792.36, 399.483),
                    new(261954.62, 7040799.52, 398.693),
                    new(261944.16, 7040824.61, 399.433),
                    new(261938.02, 7040857.14, 398.703),
                    new(261933.09, 7040866.91, 399.093),
                    new(261918.25, 7040889.2, 400.953),
                    new(261892.12, 7040925.49, 408.723),
                    new(261854.91, 7040977.33, 414.203),
                    new(261841.13, 7040991.42, 415.383),
                    new(261816.52, 7041011.47, 417.133),
                    new(261801.26, 7041029.54, 417.153),
                    new(261770.94, 7041070.51, 413.312),
                    new(261753.96, 7041091.95, 414.922),
                    new(261737.91, 7041115.44, 415.302),
                    new(261711.56, 7041148.71, 414.412),
                    new(261699.39, 7041168.49, 414.342),
                    new(261691.88, 7041180.45, 415.912),
                    new(261686.51, 7041184.57, 416.162),
                    new(261679.34, 7041185.87, 416.042),
                    new(261672.68, 7041184.99, 415.722),
                    new(261656.73, 7041175.89, 412.022),
                    new(261640.19, 7041165.99, 412.122),
                    new(261618.97, 7041142.33, 413.262),
                    new(261606.28, 7041120.25, 413.292),
                    new(261589.82, 7041097.28, 415.042),
                    new(261571.79, 7041075.96, 419.911),
                    new(261560.46, 7041066.52, 422.151),
                    new(261555.56, 7041065.36, 422.681),
                    new(261550.07, 7041067.82, 422.581),
                    new(261543.31, 7041080.5, 421.671),
                    new(261533.97, 7041091.46, 421.471),
                    new(261511.97, 7041109.87, 421.971),
                    new(261501.26, 7041114.35, 422.541),
                    new(261472.78, 7041119.47, 425.801),
                    new(261433.54, 7041126.91, 424.331),
                    new(261422.45, 7041126.73, 424.321),
                    new(261392.13, 7041122.75, 420.771),
                    new(261381.33, 7041123.88, 421.401),
                    new(261372.63, 7041127.57, 421.521),
                    new(261358.52, 7041137.7, 422.6),
                    new(261348.68, 7041148.26, 423.89),
                    new(261345.52, 7041156.14, 424.57),
                    new(261340.65, 7041179.27, 425.75),
                    new(261333.25, 7041195.52, 427.04),
                    new(261326.96, 7041201.09, 428.07),
                    new(261314.07, 7041207.45, 428.85),
                    new(261294.11, 7041211.1, 430.4),
                    new(261278.63, 7041209.4, 431.33),
                    new(261223.24, 7041193.54, 433.3),
                    new(261198.73, 7041188.35, 434.07),
                    new(261179.72, 7041188.61, 435.15),
                    new(261158.83, 7041193.31, 437.78),
                    new(261149.36, 7041192.71, 438.459),
                    new(261138.76, 7041186.81, 440.479),
                    new(261116.78, 7041168.79, 446.919),
                    new(261109.09, 7041165.7, 447.839),
                    new(261101.09, 7041165.15, 448.159),
                    new(261091.11, 7041169.25, 448.909),
                    new(261084.31, 7041179.92, 449.499),
                    new(261082.04, 7041195.76, 449.069),
                    new(261084.11, 7041213.63, 449.719),
                    new(261084.36, 7041226.53, 447.579)
                }
            },
            new()
            {
                RoadClass = 3,
                LinkId = 593586,
                FromRelativeLength = 0.041672881692647934,
                ToRelativeLength = 0.09881021827459335,
                FromNodeId = 598238,
                ToNodeId = 598244,
                RoadNumber = 0,
                Direction = RoadLinkDirection.AlongGeometry,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270161.498, 7042097.45, 7.2518926), new Point3D(270120.982, 7042073.75, 6.833) },
                LaneCode = "",
                Cost = 0.08589772135019302,
                ReverseCost = 3.4028234663852886E+38
            },
            new()
            {
                RoadClass = 3,
                LinkId = 593594,
                FromRelativeLength = 0.09881021827459335,
                ToRelativeLength = 0.1726849526166916,
                FromNodeId = 598244,
                ToNodeId = 472915,
                RoadNumber = 0,
                Direction = RoadLinkDirection.AlongGeometry,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270120.982, 7042073.75, 6.833), new Point3D(270068.314, 7042043.948, 6.433) },
                LaneCode = "",
                Cost = 0.11074263602495193,
                ReverseCost = 3.4028234663852886E+38
            },
            new()
            {
                RoadClass = 6,
                LinkId = 473188,
                FromRelativeLength = 0.8603624701499939,
                ToRelativeLength = 1,
                FromNodeId = 472907,
                ToNodeId = 472915,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270076.4, 7042017.5, 7.833), new Point3D(270068.314, 7042043.948, 6.433) },
                LaneCode = "",
                Cost = 0.06056765839457512,
                ReverseCost = 0.06056765839457512
            },
            new()
            {
                RoadClass = 6,
                LinkId = 473180,
                FromRelativeLength = 0.7119511365890503,
                ToRelativeLength = 0.8603624701499939,
                FromNodeId = 472897,
                ToNodeId = 472907,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270087.3, 7041989.1, 9.433), new Point3D(270076.4, 7042017.5, 7.833) },
                LaneCode = "",
                Cost = 0.06661956757307053,
                ReverseCost = 0.06661956757307053
            },
            new()
            {
                RoadClass = 6,
                LinkId = 1580985,
                FromRelativeLength = 0.6455956101417542,
                ToRelativeLength = 0.6962451338768005,
                FromNodeId = 1394248,
                ToNodeId = 472897,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270134, 7041993.7, 9.533), new Point3D(270087.3, 7041989.1, 9.433) },
                LaneCode = "",
                Cost = 0.1027679517865181,
                ReverseCost = 0.1027679517865181
            },
            new()
            {
                RoadClass = 6,
                LinkId = 1580977,
                FromRelativeLength = 0.5847190618515015,
                ToRelativeLength = 0.6455956101417542,
                FromNodeId = 573210,
                ToNodeId = 1394248,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270190.4, 7041991.5, 9.533), new Point3D(270134, 7041993.7, 9.533) },
                LaneCode = "",
                Cost = 0.12360993027687073,
                ReverseCost = 0.12360993027687073
            },
            new()
            {
                RoadClass = 5,
                LinkId = 564771,
                FromRelativeLength = 0.8024753928184509,
                ToRelativeLength = 0.8264882564544678,
                FromNodeId = 573210,
                ToNodeId = 573216,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270190.4, 7041991.5, 9.533), new Point3D(270186.9, 7042087.4, 7.833), new Point3D(270187.8, 7042096.4, 7.733), new Point3D(270192.6, 7042107.2, 7.633) },
                LaneCode = "",
                Cost = 0.23832783102989197,
                ReverseCost = 0.23832783102989197
            },
            new()
            {
                RoadClass = 3,
                LinkId = 593562,
                FromRelativeLength = 0,
                ToRelativeLength = 0.011160610243678093,
                FromNodeId = 573216,
                ToNodeId = 598223,
                RoadNumber = 0,
                Direction = RoadLinkDirection.AlongGeometry,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270192.6, 7042107.2, 7.633), new Point3D(270183.353, 7042108.78, 7.533) },
                LaneCode = "",
                Cost = 0.01716725341975689,
                ReverseCost = 3.4028234663852886E+38
            },
            new()
            {
                RoadClass = 3,
                LinkId = 593570,
                FromRelativeLength = 0.011160610243678093,
                ToRelativeLength = 0.03194738179445267,
                FromNodeId = 598223,
                ToNodeId = 598231,
                RoadNumber = 0,
                Direction = RoadLinkDirection.AlongGeometry,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270183.353, 7042108.78, 7.533), new Point3D(270168.405, 7042101.465, 7.333) },
                LaneCode = "",
                Cost = 0.03045462630689144,
                ReverseCost = 3.4028234663852886E+38
            },
            new()
            {
                RoadClass = 3,
                LinkId = 593578,
                FromRelativeLength = 0.03194738179445267,
                ToRelativeLength = 0.041672881692647934,
                FromNodeId = 598231,
                ToNodeId = 598238,
                RoadNumber = 0,
                Direction = RoadLinkDirection.AlongGeometry,
                SpeedLimit = 40,
                SpeedLimitReversed = 40,
                RoadType = "",
                Geometry = new[] { new Point3D(270168.405, 7042101.465, 7.333), new Point3D(270161.498, 7042097.45, 7.2518926) },
                LaneCode = "",
                Cost = 0.014620184898376465,
                ReverseCost = 3.4028234663852886E+38
            }
        });
    }
}