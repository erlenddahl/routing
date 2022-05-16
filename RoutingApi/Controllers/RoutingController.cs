using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoutingApi.Geometry;
using RoutingApi.Helpers;

namespace RoutingApi.Controllers
{
    [Route("api/[controller]")]
    public class RoutingController : Controller
    {
        public RoutingController(IConfiguration config)
        {
            LocalDijkstraRoutingService.NetworkFile = config.GetValue<string>("RoadNetworkLocation");
        }

        [HttpGet]
        public object Get()
        {
            return new
            {
                Api = "Routing",
                Version = "2"
            };
        }

        [HttpPost]
        [EnableCors("AllowAll")]
        [Route("")]
        [Route("GetRoute")]
        [AcceptVerbs("POST")]
        public object FindRoute([FromBody] List<LatLng> coordinates)
        {
            RoutingService service = null;
            try
            {
                service = RoutingService.FromLatLng(coordinates);

                return new
                {
                    distance = PointUtm33.Distance(service.Route),
                    coordinates = service.Route.Select(p => p.ToWgs84()).Select(p => new[] { Math.Round(p.Lat, 5), Math.Round(p.Lng, 5) }),
                    linkReferences = service.LinkReferences.Select(p => p.ToShortRepresentation()),
                    service.WayPointIndices,
                    service.SecsDijkstra,
                    service.SecsRetrieveLinks,
                    service.SecsModifyLinks
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    message = ex.Message
                };
            }
        }
    }
}
