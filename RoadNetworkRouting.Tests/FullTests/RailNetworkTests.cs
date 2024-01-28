using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Tests.FullTests
{
    [TestClass]
    public class RailNetworkTests
    {
        [TestMethod]
        public void Failure()
        {
            var railNetworkFile = @"C:\Users\erlendd\Desktop\Søppel\2023-12-14 - Entur, validering av jernbanenett\railNetwork.bin";
            var rail = RoutingService.Create(railNetworkFile);
            var routingConfig = new RoutingConfig()
            {
                DifferentGroupHandling = GroupHandling.BestGroup,
                MaxSearchRadius = 50_000
            };

            var inputCoordinates = new[]
            {
                new Point3D(272168.951, 7037533.496),
                new Point3D(271721.542, 7037269.011)
            };

            var route = rail.FromUtm(inputCoordinates, routingConfig,true, false);

            Debug.WriteLine(route.Links.Sum(p => p.Length));
        }

        [TestMethod]
        public void ShouldNotFail()
        {
            var railNetworkFile = @"C:\Users\erlendd\Desktop\Søppel\2023-12-14 - Entur, validering av jernbanenett\railNetwork.bin";
            var rail = RoutingService.Create(railNetworkFile);
            var routingConfig = new RoutingConfig()
            {
                DifferentGroupHandling = GroupHandling.BestGroup,
                MaxSearchRadius = 5_000
            };

            var converter = CoordinateConverter.ToUtm33(4326);

            var inputCoordinates = new[]
            {
                new Point3D(8.201103999999999, 62.258566),
                new Point3D(7.689748, 62.567260999999995)
            };
            Debug.WriteLine(converter.Forward(inputCoordinates[0]));
            Debug.WriteLine(converter.Forward(inputCoordinates[1]));

            var route = rail.FromRequest(inputCoordinates, routingConfig, converter, true, false);

            Debug.WriteLine(route.Links.Sum(p => p.Length));
        }


        [TestMethod]
        public void ShouldNotFail_Utm()
        {
            var railNetworkFile = @"C:\Users\erlendd\Desktop\Søppel\2023-12-14 - Entur, validering av jernbanenett\railNetwork.bin";
            var rail = RoutingService.Create(railNetworkFile);
            var routingConfig = new RoutingConfig()
            {
                DifferentGroupHandling = GroupHandling.BestGroup,
                MaxSearchRadius = 5_000
            };

            var inputCoordinates = new[]
            {
                new Point3D(147384.47, 6921532.92),
                new Point3D(124834.91, 6958659.40)
            };

            //rail.Router.SaveSearchDebugAsGeoJson(inputCoordinates[0], inputCoordinates[1], @"C:\Users\erlendd\Desktop\Søppel\2023-12-14 - Entur, validering av jernbanenett\search-debug", routingConfig);
            var route = rail.FromUtm(inputCoordinates, routingConfig, true, false);
            Debug.WriteLine(route.Links.Sum(p => p.Length));
        }
    }
}
