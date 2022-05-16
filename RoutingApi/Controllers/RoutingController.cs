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
            if (config != null)
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
        [AcceptVerbs("POST")]
        public object FindRoute([FromBody] RoutingRequest request)
        {
            if (request == null) return new { message = "Please provide a non-empty routing request." };
            if (request.Request != null && request.Requests != null) return new { message = "Must submit either a single Request, or multiple Requests, not Both." };

            if (request.Request != null) return RouteSingle(request.Request);

            var ix = 0;
            return request.Requests.Select(p => new
                {
                    sequence = ix++,
                    request = p
                })
                .AsParallel()
                .Select(p => new { p.sequence, result = RouteSingle(p.request) })
                .OrderBy(p => p.sequence)
                .Select(p => p.result);
        }

        private object RouteSingle(LatLng[] coordinates)
        {
            try
            {
                var service = RoutingResponse.FromLatLng(coordinates);

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

    public class RoutingRequest
    {
        public LatLng[] Request { get; set; }
        public LatLng[][] Requests { get; set; }
    }
}
