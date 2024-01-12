using System.Diagnostics;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Config;
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
            DifferentGroupHandling = GroupHandling.OnlySame,
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
}