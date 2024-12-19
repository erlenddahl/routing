using System;
using System.Diagnostics;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using Extensions.Utilities;
using Newtonsoft.Json.Linq;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;

namespace RoadNetworkRouting.Tests.FullTests;

[TestClass]
public class TramNetworkTests
{
    [TestMethod]
    public void SearchFailure_WorksWithIncreasedRadius()
    {
        var networkFile = @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\trikk-oslo.bin";
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
        //service.Router.SaveSearchDebugAsGeoJson(converter.Forward(inputCoordinates[0]), converter.Forward(inputCoordinates[1]), @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\Debugging\tram", routingConfig, timer); 
        
        var route = service.FromRequest(inputCoordinates, routingConfig, converter, true, false, timer);
        
        Assert.IsTrue(converter.Forward(inputCoordinates[0]).DistanceTo2D(route.Coordinates[0]) < 50);
        Assert.IsTrue(converter.Forward(inputCoordinates[^1]).DistanceTo2D(route.Coordinates[^1]) < 50);
    }

    [TestMethod]
    public void TramNetwork_OnlyOneNetworkGroup()
    {

        var networkFile = @"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\trikk-oslo.bin";
        var service = RoutingService.Create(networkFile);
        
        Assert.AreEqual(1, service.Router.Links.Select(p=>p.Value.NetworkGroup).Distinct().Count());
    }

    [TestMethod]
    public void CreateFromGeoJsonWorks()
    {
        var converter = CoordinateConverter.ToUtm33(4326);
        var router = RoadNetworkRouter.BuildFromGeoJsonLines(@"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\trikk-oslo.geojsonl.json", (x, y) =>
        {
            // In some of the geojson files, the coordinates are already UTM33

            if (x > 200 || y > 200)
            {
                throw new Exception("Already UTM!");
            }

            var utm = converter.Forward(x, y);
            return (utm.X, utm.Y);
        }, new TramRoutingNetworkExtractor());
    }

    [TestMethod]
    public void TinySubset_OneNetworkGroup()
    {
        var converter = CoordinateConverter.ToUtm33(4326);
        var router = RoadNetworkRouter.BuildFromGeoJsonLines(@"C:\Users\erlendd\Desktop\Søppel\2024-09-12 - Sporveien\trikk-test-subsett.geojsonl.json", (x, y) =>
        {
            // In some of the geojson files, the coordinates are already UTM33

            if (x > 200 || y > 200)
            {
                throw new Exception("Already UTM!");
            }

            var utm = converter.Forward(x, y);
            return (utm.X, utm.Y);
        }, new TramRoutingNetworkExtractor());

        var analysis = router.Graph.Analyze();
        Assert.AreEqual(1, analysis.TotalNumberOfGroups);
    }

    public class TramRoutingNetworkExtractor : GeoJsonValueExtractor
    {
        private int _linkId = 0;
        private CoordinateConverter converter = CoordinateConverter.ToUtm33(4326);

        public override int GetFromNodeId(JToken properties) => int.MinValue;
        public override int GetToNodeId(JToken properties) => int.MinValue;
        public override int GetLinkId(JToken properties) => _linkId++;
        public override byte GetSpeedLimitForward(JToken properties) => 40;
        public override byte GetSpeedLimitBackwards(JToken properties) => 40;
        public override float GetCostForward(JToken properties) => (float)LineTools.CalculateLength(GetGeometry(properties)["coordinates"].Value<JArray>().Select(p => converter.Forward(new Point3D(p[0].Value<double>(), p[1].Value<double>(), p[2].Value<double>()))).ToArray());

        JToken GetGeometry(JToken properties)
        {
            var json = properties.Parent.Parent.Value<JToken>("geometry");
            if (json.Type != JTokenType.Null) return json;

            throw new Exception("Failed to find geometry.");
        }

        public override float GetCostBackwards(JToken properties) => (float)LineTools.CalculateLength(GetGeometry(properties)["coordinates"].Value<JArray>().Select(p => converter.Forward(new Point3D(p[0].Value<double>(), p[1].Value<double>(), p[2].Value<double>()))).ToArray());
        public override double GetFromRelativeLength(JToken properties) => 0;
        public override double GetToRelativeLength(JToken properties) => 1;
        public override byte GetRoadClass(JToken properties) => 0;
        public override RoadLinkDirection GetDirection(JToken properties) => RoadLinkDirection.BothWays;
        public override string GetLaneCode(JToken properties) => null;
        public override float GetRoadWidth(JToken properties) => 8;
        public override bool IsFerry(JToken properties) => false;
        public override bool IsRoundabout(JToken properties) => false;
        public override bool IsBridge(JToken properties) => false;
        public override bool IsTunnel(JToken properties) => false;
        public override bool IgnoreLink(JToken properties) => !string.IsNullOrWhiteSpace(properties.Value<string>("Ignore"));
    }


}