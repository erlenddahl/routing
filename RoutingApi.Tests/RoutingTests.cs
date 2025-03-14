using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;
using RoutingApi.Controllers;

namespace RoutingApi.Tests
{
    [TestClass]
    public class RoutingTests
    {
        [TestInitialize]
        public void Init()
        {
            RoutingController.Service = RoutingService.Create("D:\\Lager\\RouteNetworkUpdater\\2023-01-09\\roadNetwork.bin");
        }

        [TestMethod]
        public void ShortRoute_AvoidBackAndForth()
        {
            // In the initial version, the router took U-turns both at the start and the end of this route.
            // (The problem was that some of the links weren't rotated correctly.)

            var response = new SingleRoutingRequest()
            {
                SourceSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    LinkReferences = true,
                    Coordinates = true
                },
                OutputSrid = 4326,
                Waypoints = new[]
                {
                    new Point3D(10.415004587765168, 63.41784215066588),
                    new Point3D(10.414155474818585, 63.4179078153318),
                }
            }.Route(RoutingController.Service).CheckThrow();

            Assert.AreEqual(62, response.DistanceM, 0.5);
            Assert.AreEqual(7, response.Coordinates.Count);
            Assert.AreEqual(3, response.LinkReferences.Count);

            //foreach (var c in response.Coordinates)
            //    Debug.WriteLine(c.X + ";" + c.Y);

            var referenceString = "0,36020569-0,52123529@622787-2;0,48851538-0,50344980@1548623-2;0,00000000-0,28672132@490120-2";
            Assert.AreEqual(referenceString, string.Join(";", response.LinkReferences));
        }

        [TestMethod]
        public void LongRouteWithWeirdBehaviourBecauseOfDifferentGroups()
        {
            // This is a very long route, but the links that are nearest the search points 
            // are in their own groups (due to missing connections in the network, I believe),
            // so that they are mapped to one of these links, resulting in a tiny route instead.
            // Just to be clear: this is an issue with the road network.
            // (Although it could also be resolved by routing between groups, but that's a lot of work.)

            var response = new SingleRoutingRequest()
            {
                SourceSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    LinkReferences = true,
                    Coordinates = true
                },
                OutputSrid = 4326,
                RoutingConfig = new RoutingConfig()
                {
                    DifferentGroupHandling = GroupHandling.BestGroup,
                    MaxSearchRadius = int.MaxValue
                },
                Waypoints = new[]
                {
                    new Point3D(10.339970694099293, 63.63778918654236),
                    new Point3D(10.973513560882651, 63.670846973692676),
                }
            }.Route(RoutingController.Service).CheckThrow();

            Assert.AreEqual(2, response.LinkReferences.Count);
            Assert.AreEqual(457, response.DistanceM, 1);
        }

        [TestMethod]
        public void LongRoute()
        {
            // Almost the same route as above, but moved the search points slightly
            // to hit the main network group.

            var response = new SingleRoutingRequest()
            {
                SourceSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    LinkReferences = true,
                    Coordinates = true
                },
                OutputSrid = 4326,
                RoutingConfig = new RoutingConfig()
                {
                    DifferentGroupHandling = GroupHandling.BestGroup,
                    MaxSearchRadius = int.MaxValue
                },
                Waypoints = new[]
                {
                    new Point3D(10.324528, 63.653061),
                    new Point3D(10.969157,63.698317),
                }
            }.Route(RoutingController.Service).CheckThrow();

            Assert.AreEqual(125000, response.DistanceM, 10_000);
            Assert.AreEqual(4000, response.Coordinates.Count, 500);
            Assert.AreEqual(677, response.LinkReferences.Count, 100);
        }

        [TestMethod]
        public void SingleRoute()
        {
            var response = new SingleRoutingRequest()
            {
                SourceSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    LinkReferences = true,
                    Coordinates = true
                },
                OutputSrid = 4326,
                Waypoints = new[]
                {
                    new Point3D(10.412028, 63.413602),
                    new Point3D(10.350386, 63.399147),
                }
            }.Route(RoutingController.Service).CheckThrow();

            Assert.AreEqual(7750, response.DistanceM, 500);
            Assert.AreEqual(436, response.Coordinates.Count, 100);
            Assert.AreEqual(187, response.LinkReferences.Count, 50);

            //foreach (var c in response.Coordinates)
                //Debug.WriteLine(c.X + ";" + c.Y);
        }

        [TestMethod]
        public void MultipleIdenticalRoutes()
        {
            var responses = new MultiRoutingRequest()
            {
                SourceSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    LinkReferences = true,
                    Coordinates = true,
                    Links = true
                },
                OutputSrid = 25833,
                Waypoints = Enumerable.Range(0, 20)
                    .Select(p => new[]
                    {
                        new Point3D(10.412028, 63.413602),
                        new Point3D(10.350386, 63.399147),
                    })
                    .ToArray()
            }.Route(RoutingController.Service).ToArray();
            
            Assert.AreEqual(20, responses.Length);

            foreach (var response in responses)
            {
                response.CheckThrow();
                Assert.AreEqual(7750, response.DistanceM, 500);
                Assert.AreEqual(436, response.Coordinates.Count, 100);
                Assert.AreEqual(187, response.LinkReferences.Count, 50);
            }
        }


        [TestMethod]
        public void ResponseSequence()
        {
            var responses = new MultiRoutingRequest()
            {
                SourceSrid = 4326,
                OutputSrid = 4326,
                Response = new RoutingResponseDefinition()
                {
                    RequestedWaypoints = true
                },
                RoutingConfig = new RoutingConfig()
                {
                    MaxSearchRadius = 100_000
                },
                Waypoints = new []
                {
                    new[]
                    {
                        new Point3D(10.1, 63.1),
                        new Point3D(10.350386, 63.399147)
                    },
                    new[]
                    {
                        new Point3D(10.2, 63.2),
                        new Point3D(10.350386, 63.399147)
                    },
                    new[]
                    {
                        new Point3D(10.3, 63.3),
                        new Point3D(10.350386, 63.399147)
                    },
                    new[]
                    {
                        new Point3D(10.4, 63.4),
                        new Point3D(10.350386, 63.399147)
                    },
                    new[]
                    {
                        new Point3D(10.5, 63.5),
                        new Point3D(10.350386, 63.399147)
                    }
                }
            }.Route(RoutingController.Service).ToArray();

            for (var i = 0; i < responses.Count(); i++)
            {
                Console.WriteLine(responses[i].RequestedWaypoints[0].FromWaypoint.Y);
            }

            Assert.AreEqual(5, responses.Count());

            for (var i = 0; i < responses.Count(); i++)
            {
                responses[i].CheckThrow();
                Assert.AreEqual(63d + (i + 1d) / 10d, responses[i].RequestedWaypoints[0].FromWaypoint.Y, 0.0005);
            }
        }

        [TestMethod]
        public void MinimumRequest()
        {
            var response = new SingleRoutingRequest()
            {
                Waypoints = new[]
                {
                    new Point3D(271047.81, 7039885.66),
                    new Point3D(269319.20, 7039903.40),
                }
            }.Route(RoutingController.Service).CheckThrow();

            Assert.AreEqual(6500, response.DistanceM, 500);
            Assert.IsNotNull(response.Coordinates);
            Assert.IsNull(response.LinkReferences);
        }
    }
}
