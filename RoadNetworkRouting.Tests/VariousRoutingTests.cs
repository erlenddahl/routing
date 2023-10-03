using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class VariousRoutingTests
{
    [TestMethod]
    public void CuttingWhenOverloaderChangesOneOfTheLinks_SeparateGroups()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 555155,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 563968,
                ToNodeId = 563969,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 50,
                SpeedLimitReversed = 50,
                RoadType = "",
                Geometry = new[] { new Point3D(268813.76, 7066437.46, 237.267), new Point3D(268810.17, 7066418.56, 237.318), new Point3D(268809.49, 7066401.21, 236.968), new Point3D(268812.37, 7066387.61, 237.148), new Point3D(268820.88, 7066365.45, 237.578), new Point3D(268824.98, 7066339.91, 240.598), new Point3D(268823.64, 7066313.13, 243.868), new Point3D(268818.57, 7066279.75, 243.788), new Point3D(268818.14, 7066237.12, 247.749), new Point3D(268822.48, 7066223.57, 248.909), new Point3D(268836.58, 7066202.64, 250.829), new Point3D(268840.03, 7066193.64, 252.159), new Point3D(268840.53, 7066179.8, 253.609), new Point3D(268835.35, 7066133.45, 261.949), new Point3D(268839.8, 7066106.7, 266.06), new Point3D(268840.97, 7066070.47, 268.22), new Point3D(268844.77, 7066052.79, 271.39), new Point3D(268856.12, 7066030.75, 275.77), new Point3D(268862.65, 7066014.39, 277.97), new Point3D(268867.08, 7065997.95, 279.02), new Point3D(268867.72, 7065987.94, 279.49), new Point3D(268866.04, 7065970.71, 279.41), new Point3D(268862.75, 7065958.65, 278.101), new Point3D(268856.34, 7065952.28, 276.841), new Point3D(268830.9, 7065936.15, 272.171), new Point3D(268820.78, 7065926.57, 270.651), new Point3D(268812.39, 7065912.85, 269.801), new Point3D(268808.05, 7065896.45, 269.041), new Point3D(268807.07, 7065858.98, 266.921), new Point3D(268809.8, 7065818.23, 263.011), new Point3D(268809.36, 7065771.14, 261.271), new Point3D(268807.27, 7065739.44, 260.352), new Point3D(268809.41, 7065726.34, 260.492), new Point3D(268819.62, 7065698.96, 261.462), new Point3D(268821.94, 7065685.85, 261.782), new Point3D(268821.06, 7065673.99, 261.842), new Point3D(268816.39, 7065653.21, 262.182), new Point3D(268810, 7065631.6, 264.592), new Point3D(268809.06, 7065621.48, 265.542), new Point3D(268811.89, 7065608.03, 267.072), new Point3D(268822.63, 7065586.05, 269.563), new Point3D(268823.83, 7065569.77, 270.283), new Point3D(268822.36, 7065558.42, 270.563), new Point3D(268816.2, 7065540.52, 271.393), new Point3D(268795.84, 7065516.25, 271.533), new Point3D(268791.28, 7065503.78, 271.653), new Point3D(268788, 7065449.19, 270.643), new Point3D(268788.55, 7065389.21, 274.854), new Point3D(268787.95, 7065360.38, 274.784), new Point3D(268784.36, 7065314.82, 279.374), new Point3D(268781.44, 7065270.34, 280.574), new Point3D(268774.74, 7065224.15, 284.375), new Point3D(268767.65, 7065196.82, 286.675), new Point3D(268759.81, 7065167.36, 286.505), new Point3D(268756.05, 7065127.42, 283.615), new Point3D(268749.9, 7065092, 281.845), new Point3D(268754.73, 7065033.54, 284.566), new Point3D(268765.61, 7064986.18, 282.986), new Point3D(268770.16, 7064959.94, 281.436), new Point3D(268775.17, 7064921.69, 281.516), new Point3D(268774.87, 7064903.17, 280.196), new Point3D(268769.03, 7064886.03, 277.696), new Point3D(268759.9, 7064875.82, 276.477), new Point3D(268731.41, 7064856.16, 276.537), new Point3D(268704.47, 7064841.66, 279.156), new Point3D(268690.66, 7064830.61, 281.626), new Point3D(268676.25, 7064814.85, 283.727), new Point3D(268667.42, 7064802.25, 284.557), new Point3D(268659.63, 7064786.54, 285.347), new Point3D(268643.74, 7064724.96, 290.047), new Point3D(268642.77, 7064714.23, 289.867), new Point3D(268646.79, 7064668.6, 288.367), new Point3D(268647.46, 7064618.74, 281.568), new Point3D(268650.39, 7064610.19, 280.938), new Point3D(268671.21, 7064570.39, 278.418), new Point3D(268679.5, 7064550.18, 279.058), new Point3D(268682.82, 7064525.6, 280.828), new Point3D(268686.07, 7064503.82, 280.408), new Point3D(268687.7, 7064471.33, 278.599) },
                LaneCode = "",
                Cost = 3.6848573684692383,
                ReverseCost = 3.6848573684692383,
                NetworkGroup = 1
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 574245,
                FromRelativeLength = 0.31089574098587036,
                ToRelativeLength = 0.869411289691925,
                FromNodeId = 582150,
                ToNodeId = 582158,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(300679.86, 7068780.97, 163.38), new Point3D(300700.6, 7068755.3, 166.14), new Point3D(300703.2, 7068741, 168.14), new Point3D(300704.4, 7068711.7, 171.14), new Point3D(300697.2, 7068666.1, 173.14), new Point3D(300679, 7068633.7, 173.14), new Point3D(300673.5, 7068617.2, 173.14), new Point3D(300671.3, 7068563.1, 174.14), new Point3D(300664.1, 7068539.7, 174.14), new Point3D(300661.2, 7068519.8, 174.14), new Point3D(300662, 7068505.7, 173.14), new Point3D(300652.8, 7068461.3, 172.139), new Point3D(300640.4, 7068416.3, 170.139), new Point3D(300639, 7068400.3, 169.139), new Point3D(300640.1, 7068391.2, 169.139), new Point3D(300648.5, 7068363.3, 168.139), new Point3D(300656.9, 7068345.4, 167.139), new Point3D(300663.9, 7068323.7, 166.139), new Point3D(300663.3, 7068317.7, 165.139), new Point3D(300659, 7068304.1, 165.139), new Point3D(300641.1, 7068263.6, 161.139), new Point3D(300625.1, 7068231.9, 156.139), new Point3D(300610.8, 7068208.1, 154.139), new Point3D(300598.2, 7068181.1, 150.139), new Point3D(300568.3, 7068119.7, 148.139), new Point3D(300550.1, 7068044, 145.139), new Point3D(300544.1, 7068023.5, 145.139), new Point3D(300544.5, 7068005.3, 145.139), new Point3D(300547.3, 7067982, 144.139), new Point3D(300541, 7067947.4, 143.139), new Point3D(300526, 7067915.6, 143.139), new Point3D(300512, 7067894.8, 142.139), new Point3D(300479.8, 7067862.7, 142.139), new Point3D(300472.2, 7067845.4, 143.138), new Point3D(300459.7, 7067809.3, 146.138), new Point3D(300457.6, 7067786.4, 147.138), new Point3D(300458.8, 7067767.2, 147.138), new Point3D(300456.4, 7067741.3, 146.138), new Point3D(300448, 7067685.8, 144.138), new Point3D(300435.7, 7067628.7, 145.138), new Point3D(300428.9, 7067599.2, 143.138), new Point3D(300423.3, 7067550.6, 143.138), new Point3D(300419.3, 7067529.8, 143.138), new Point3D(300410.8, 7067503.5, 143.138), new Point3D(300400.3, 7067477.3, 144.138), new Point3D(300377.6, 7067428.2, 145.138), new Point3D(300371.2, 7067402.7, 145.138), new Point3D(300369.3, 7067372.8, 144.138), new Point3D(300383.4, 7067318.2, 143.138), new Point3D(300383.6, 7067300, 143.138), new Point3D(300375.5, 7067213.4, 145.138), new Point3D(300367.9, 7067153.9, 144.137), new Point3D(300363.9, 7067144.2, 142.137), new Point3D(300356.2, 7067115.7, 139.137), new Point3D(300348.8, 7067101.4, 139.137), new Point3D(300305.7, 7067038.1, 140.137), new Point3D(300295.9, 7067020, 141.137), new Point3D(300283.9, 7067010, 141.137) },
                LaneCode = "",
                Cost = 11.07801628112793,
                ReverseCost = 11.07801628112793,
                NetworkGroup = 2
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 574253,
                FromRelativeLength = 0.869411289691925,
                ToRelativeLength = 1,
                FromNodeId = 582158,
                ToNodeId = 582165,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 50,
                SpeedLimitReversed = 50,
                RoadType = "",
                Geometry = new[] { new Point3D(300283.9, 7067010, 141.137), new Point3D(300209.3, 7066953.7, 143.137), new Point3D(300188.5, 7066934.6, 144.137), new Point3D(300147.6, 7066917.3, 145.137), new Point3D(300130.7, 7066908.8, 145.137), new Point3D(300123.1, 7066902.5, 145.137), new Point3D(300108.5, 7066885.9, 145.137), new Point3D(300097.9, 7066869.7, 145.137), new Point3D(300092.5, 7066865.3, 146.137), new Point3D(300054.2, 7066821.7, 152.137), new Point3D(300018.5, 7066774.7, 158.137), new Point3D(299985, 7066726.7, 164.136), new Point3D(299971.4, 7066710.8, 165.136) },
                LaneCode = "",
                Cost = 0.7758787870407104,
                ReverseCost = 0.7758787870407104,
                NetworkGroup = 2
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(269276.2361452655, 7065066.461833899),
            new Point3D(300841.92263974645, 7066612.177553299)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup, MaxSearchRadius = 10_000_000});

        Assert.AreEqual(2, res.Links.Length);
        Assert.AreEqual(457, res.RouteDistance, 1);
    }


    [TestMethod]
    public void CuttingWhenOverloaderChangesOneOfTheLinks_SameGroups()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 574245,
                FromRelativeLength = 0.31089574098587036,
                ToRelativeLength = 0.869411289691925,
                FromNodeId = 582150,
                ToNodeId = 582158,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(300679.86, 7068780.97, 163.38), new Point3D(300700.6, 7068755.3, 166.14), new Point3D(300703.2, 7068741, 168.14), new Point3D(300704.4, 7068711.7, 171.14), new Point3D(300697.2, 7068666.1, 173.14), new Point3D(300679, 7068633.7, 173.14), new Point3D(300673.5, 7068617.2, 173.14), new Point3D(300671.3, 7068563.1, 174.14), new Point3D(300664.1, 7068539.7, 174.14), new Point3D(300661.2, 7068519.8, 174.14), new Point3D(300662, 7068505.7, 173.14), new Point3D(300652.8, 7068461.3, 172.139), new Point3D(300640.4, 7068416.3, 170.139), new Point3D(300639, 7068400.3, 169.139), new Point3D(300640.1, 7068391.2, 169.139), new Point3D(300648.5, 7068363.3, 168.139), new Point3D(300656.9, 7068345.4, 167.139), new Point3D(300663.9, 7068323.7, 166.139), new Point3D(300663.3, 7068317.7, 165.139), new Point3D(300659, 7068304.1, 165.139), new Point3D(300641.1, 7068263.6, 161.139), new Point3D(300625.1, 7068231.9, 156.139), new Point3D(300610.8, 7068208.1, 154.139), new Point3D(300598.2, 7068181.1, 150.139), new Point3D(300568.3, 7068119.7, 148.139), new Point3D(300550.1, 7068044, 145.139), new Point3D(300544.1, 7068023.5, 145.139), new Point3D(300544.5, 7068005.3, 145.139), new Point3D(300547.3, 7067982, 144.139), new Point3D(300541, 7067947.4, 143.139), new Point3D(300526, 7067915.6, 143.139), new Point3D(300512, 7067894.8, 142.139), new Point3D(300479.8, 7067862.7, 142.139), new Point3D(300472.2, 7067845.4, 143.138), new Point3D(300459.7, 7067809.3, 146.138), new Point3D(300457.6, 7067786.4, 147.138), new Point3D(300458.8, 7067767.2, 147.138), new Point3D(300456.4, 7067741.3, 146.138), new Point3D(300448, 7067685.8, 144.138), new Point3D(300435.7, 7067628.7, 145.138), new Point3D(300428.9, 7067599.2, 143.138), new Point3D(300423.3, 7067550.6, 143.138), new Point3D(300419.3, 7067529.8, 143.138), new Point3D(300410.8, 7067503.5, 143.138), new Point3D(300400.3, 7067477.3, 144.138), new Point3D(300377.6, 7067428.2, 145.138), new Point3D(300371.2, 7067402.7, 145.138), new Point3D(300369.3, 7067372.8, 144.138), new Point3D(300383.4, 7067318.2, 143.138), new Point3D(300383.6, 7067300, 143.138), new Point3D(300375.5, 7067213.4, 145.138), new Point3D(300367.9, 7067153.9, 144.137), new Point3D(300363.9, 7067144.2, 142.137), new Point3D(300356.2, 7067115.7, 139.137), new Point3D(300348.8, 7067101.4, 139.137), new Point3D(300305.7, 7067038.1, 140.137), new Point3D(300295.9, 7067020, 141.137), new Point3D(300283.9, 7067010, 141.137) },
                LaneCode = "",
                Cost = 11.07801628112793,
                ReverseCost = 11.07801628112793,
                NetworkGroup = 2
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 574253,
                FromRelativeLength = 0.869411289691925,
                ToRelativeLength = 1,
                FromNodeId = 582158,
                ToNodeId = 582165,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 50,
                SpeedLimitReversed = 50,
                RoadType = "",
                Geometry = new[] { new Point3D(300283.9, 7067010, 141.137), new Point3D(300209.3, 7066953.7, 143.137), new Point3D(300188.5, 7066934.6, 144.137), new Point3D(300147.6, 7066917.3, 145.137), new Point3D(300130.7, 7066908.8, 145.137), new Point3D(300123.1, 7066902.5, 145.137), new Point3D(300108.5, 7066885.9, 145.137), new Point3D(300097.9, 7066869.7, 145.137), new Point3D(300092.5, 7066865.3, 146.137), new Point3D(300054.2, 7066821.7, 152.137), new Point3D(300018.5, 7066774.7, 158.137), new Point3D(299985, 7066726.7, 164.136), new Point3D(299971.4, 7066710.8, 165.136) },
                LaneCode = "",
                Cost = 0.7758787870407104,
                ReverseCost = 0.7758787870407104,
                NetworkGroup = 2
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(269276.2361452655, 7065066.461833899),
            new Point3D(300841.92263974645, 7066612.177553299)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup, MaxSearchRadius = 10_000_000});

        Assert.AreEqual(2, res.Links.Length);
        Assert.AreEqual(457, res.RouteDistance, 1);
    }


    [TestMethod]
    public void OverloadingOnBothSides_EntireLinkInTheMiddle()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 3,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 12,
                ToNodeId = 13,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(300, 0), new Point3D(400,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(150, 10),
            new Point3D(350, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(3, res.Links.Length);

        Assert.AreEqual(50, res.Links[0].Length);
        Assert.AreEqual(100, res.Links[1].Length);
        Assert.AreEqual(50, res.Links[2].Length);

        Assert.AreEqual(200, res.RouteDistance);

        Assert.AreEqual(150, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);
                                           
        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(300, res.Links[1].Geometry[1].X);
                                           
        Assert.AreEqual(300, res.Links[2].Geometry[0].X);
        Assert.AreEqual(350, res.Links[2].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoHalfLinks()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(150, 10),
            new Point3D(250, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(50, res.Links[0].Length);
        Assert.AreEqual(50, res.Links[1].Length);

        Assert.AreEqual(100, res.RouteDistance);

        Assert.AreEqual(150, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(250, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneThirdEach()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(55, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_BC()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(200, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(1, res.Links.Length);

        Assert.AreEqual(25, res.Links[0].Length);

        Assert.AreEqual(25, res.RouteDistance);

        Assert.AreEqual(200, res.Links[0].Geometry[0].X);
        Assert.AreEqual(225, res.Links[0].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_AB()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(200, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(1, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);

        Assert.AreEqual(30, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_BarelyIntoTheFirst()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(199.99, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(0.01, res.Links[0].Length, 0.005);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(25.01, res.RouteDistance, 0.005);

        Assert.AreEqual(199.99, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_BarelyIntoTheSecond()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(200.01, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(0.01, res.Links[1].Length, 0.005);

        Assert.AreEqual(30.01, res.RouteDistance, 0.005);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(200.01, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneMeterIntoTheFirst()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(199, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(1, res.Links[0].Length);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(26, res.RouteDistance);

        Assert.AreEqual(199, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneMeterIntoTheSecond()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(201, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(1, res.Links[1].Length);

        Assert.AreEqual(31, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(201, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneThirdEach_SecondReversed()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 10,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(100, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 12,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] {new Point3D(300, 0), new Point3D(200, 0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(55, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneThirdEach_FirstReversed()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 10,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] {new Point3D(200, 0), new Point3D(100, 0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 12,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(300,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(55, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }


    [TestMethod]
    public void OverloadingOnBothSides_TwoLinks_OneThirdEach_BothReversed()
    {
        var links = new[]
        {
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 1,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 11,
                ToNodeId = 10,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(200, 0), new Point3D(100,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            },
            new RoadLink()
            {
                RoadClass = 7,
                LinkId = 2,
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                FromNodeId = 12,
                ToNodeId = 11,
                RoadNumber = 0,
                Direction = RoadLinkDirection.BothWays,
                SpeedLimit = 15,
                SpeedLimitReversed = 15,
                RoadType = "",
                Geometry = new[] { new Point3D(300, 0), new Point3D(200,0) },
                LaneCode = "",
                Cost = 10,
                ReverseCost = 10
            }
        };
        var router = new RoadNetworkRouter(links);
        var waypoints = new[]
        {
            new Point3D(170, 10),
            new Point3D(225, -10)
        };

        var res = router.Search(waypoints[0], waypoints[1], new RoutingConfig() { DifferentGroupHandling = GroupHandling.BestGroup });

        Assert.AreEqual(2, res.Links.Length);

        Assert.AreEqual(30, res.Links[0].Length);
        Assert.AreEqual(25, res.Links[1].Length);

        Assert.AreEqual(55, res.RouteDistance);

        Assert.AreEqual(170, res.Links[0].Geometry[0].X);
        Assert.AreEqual(200, res.Links[0].Geometry[1].X);

        Assert.AreEqual(200, res.Links[1].Geometry[0].X);
        Assert.AreEqual(225, res.Links[1].Geometry[1].X);
    }
}