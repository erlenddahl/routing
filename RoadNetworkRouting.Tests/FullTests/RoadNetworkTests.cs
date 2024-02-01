using System.Diagnostics;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.Utilities;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Exceptions;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;

namespace RoadNetworkRouting.Tests.FullTests;

[TestClass]
public class RoadNetworkTests
{
    [TestMethod]
    public void ShouldNotFail()
    {
        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000
        };

        var inputCoordinates = new[]
        {
            new Point3D(261608.953, 6649179.325, 0.000),
            new Point3D(261280.470, 6649196.625, 0.000)
        };

        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false);

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator:Environment.NewLine));
    }

    [TestMethod]
    [ExpectedException(typeof(RoutingException))]
    public void FailingRoute()
    {
        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000,
            MaxSearchDurationMs = double.MaxValue
        };

        var inputCoordinates = new[]
        {
            new Point3D(262961.024, 6649164.592, 0.000),
            new Point3D(261975.168, 6648283.204, 0.000)
        };

        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false, id: "failing-route");

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator: Environment.NewLine));
    }

    [TestMethod]
    [ExpectedException(typeof(RoutingException))]
    public void FailingRoute2()
    {
        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000,
            MaxSearchDurationMs = double.MaxValue
        };

        var inputCoordinates = new[]
        {
            new Point3D(734443.354, 7667478.805, 0.000),
            new Point3D(734443.354, 7667478.805, 0.000)
        };

        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false, id: "too-near");

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator: Environment.NewLine));
    }

    [TestMethod]
    public void RouteThatFailedBefore_NowFixed()
    {
        // This route previously failed because it was along a single road link, which
        // was completely cut away due to a bug in the cutting.

        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000,
            MaxSearchDurationMs = double.MaxValue
        };

        var inputCoordinates = new[]
        {
            new Point3D(310243.750, 6678327.435, 0.000),
            new Point3D(310275.948, 6678327.102, 0.000)
        };

        var timer = new TaskTimer();
        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig, timer);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false, id: "fails-in-17");

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator: Environment.NewLine));
    }

    [TestMethod]
    public void RouteThatFailedBefore_NowFixed2()
    {
        // This route previously failed because it was along a single road link, which
        // was completely cut away due to a bug in the cutting.

        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000,
            MaxSearchDurationMs = double.MaxValue
        };

        var inputCoordinates = new[]
        {
            new Point3D(244001.863, 6701709.284, 0.000),
            new Point3D(244087.737, 6701245.867, 0.000)
        };

        var timer = new TaskTimer();
        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig, timer);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false, id: "fails-in-17");

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator: Environment.NewLine));
    }

    [TestMethod]
    public void FixedRoute()
    {
        // This used to fail because of an error in the binary search in NearbyCache, resulting in both search points
        // picking the same (wrong) road link as their entry point. Should be fixed now.

        var roadNetworkFile = @"C:\Code\EnergyModule\EnergyModuleGeneralRestApi\roadNetwork.bin";
        var road = RoutingService.Create(roadNetworkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 5_000,
            MaxSearchDurationMs = double.MaxValue
        };

        var inputCoordinates = new[]
        {
            new Point3D(-41699.806, 6560790.414, 0.000),
            new Point3D(-41532.049, 6560489.327, 0.000)
        };

        //road.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2024-01-12 - Entur, debugging av feil-ytelse\search-debug", routingConfig);
        var route = road.FromUtm(inputCoordinates, routingConfig, true, false, id: "same-point");

        Debug.WriteLine(route.Links.Sum(p => p.Length));
        Debug.WriteLine(route.Timings.ToString(lineSeparator: Environment.NewLine));
        Assert.AreEqual(500, route.Links.Sum(p => p.Length), 30);
    }
}