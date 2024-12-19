using EnergyModule.Geometry.SimpleStructures;
using Extensions.Utilities;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;

namespace RoadNetworkRouting.Tests.FullTests;

[TestClass]
public class SubwayNetworkTests
{
    [TestMethod]
    public void SearchFailure_WorksWithIncreasedRadius()
    {
        var networkFile = @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\t-bane-oslo-processed.bin";
        var service = RoutingService.Create(networkFile);
        var routingConfig = new RoutingConfig()
        {
            DifferentGroupHandling = GroupHandling.BestGroup,
            MaxSearchRadius = 50_000
        };

        var inputCoordinates = new[]
        {
            new Point3D(10.714356736595306, 59.928891248421365),
            new Point3D(10.780694930520326, 59.945464222040755)
        };

        var converter = CoordinateConverter.ToUtm33(4326);

        var timer = new TaskTimer();
        service.Router.SaveSearchDebugAsGeoJson(converter.Forward(inputCoordinates[0]), converter.Forward(inputCoordinates[1]), @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\Debugging\subway", routingConfig, timer); 

        var route = service.FromRequest(inputCoordinates, routingConfig, converter, true, false, timer);

        var diffA = converter.Forward(inputCoordinates[0]).DistanceTo2D(route.Coordinates[0]);
        var diffB = converter.Forward(inputCoordinates[^1]).DistanceTo2D(route.Coordinates[^1]);
        Assert.IsTrue(diffA < 150);
        Assert.IsTrue(diffB < 150);
    }

    [TestMethod]
    public void SubwayNetwork_OnlyOneNetworkGroup()
    {

        var networkFile = @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\t-bane-oslo-processed.bin";
        var service = RoutingService.Create(networkFile);

        Assert.AreEqual(1, service.Router.Links.Select(p => p.Value.NetworkGroup).Distinct().Count());
    }
}