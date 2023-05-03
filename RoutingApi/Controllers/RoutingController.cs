using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoadNetworkRouting.Config;
using RoutingApi.Geometry;
using RoutingApi.Helpers;

namespace RoutingApi.Controllers
{
    [Route("api/[controller]")]
    [EnableCors]
    public class RoutingController : Controller
    {
        public RoutingController(IConfiguration config)
        {
            LocalDijkstraRoutingService.InitializeFromConfig(config);
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
        [AcceptVerbs("POST")]
        public object FindRoute([FromBody] RoutingRequest request)
        {
            if (request == null) return new { message = "Please provide a non-empty routing request." };

            var requestTypes = new[] { request.Request != null, request.Requests != null, request.AllToAllRequests != null };

            if(requestTypes.Count(p=>p)>1)
                return new { message = "The request object cannot have more than one request type (Request, Requests, or AllToAllRequests)." };

            if (request.Request != null) return RouteSingle(request.Request, request.RoutingConfig);
            if (request.AllToAllRequests != null) return RouteAllToAll(request.AllToAllRequests, request.RoutingConfig);
            if (request.Requests != null) return RouteMultiple(request.Requests, request.RoutingConfig);

            return new { message = "The request object must either have the Request property, the Requests property, or the AllToAllRequests property." };
        }

        private object RouteMultiple(RequestCoordinate[][] requests, RoutingConfig routingConfig = null)
        {
            var ix = 0;
            return requests.Select(p => new
                {
                    sequence = ix++,
                    request = p
                })
                .AsParallel()
                .Select(p => new { p.sequence, result = RouteSingle(p.request, routingConfig) })
                .OrderBy(p => p.sequence)
                .Select(p => p.result);
        }

        private object RouteAllToAll(RequestCoordinate[] requests, RoutingConfig routingConfig = null)
        {
            var ix = 0;
            return requests.Select(p => new
                {
                    sequence = ix++,
                    source = p
                })
                .AsParallel()
                .Select(p => new { p.sequence, results = RouteOneToAll(p.source, requests, routingConfig) })
                .OrderBy(p => p.sequence)
                .Select(p => p.results);
        }

        private object RouteOneToAll(RequestCoordinate source, RequestCoordinate[] targets, RoutingConfig routingConfig = null)
        {
            throw new NotImplementedException();
        }

        private object RouteSingle(RequestCoordinate[] coordinates, RoutingConfig routingConfig = null)
        {
            try
            {
                var service = RoutingResponse.FromRequestCoordinates(coordinates, routingConfig);

                return new
                {
                    distance = PointUtm33.Distance(service.Route),
                    coordinates = service.Route.Select(p => p.ToWgs84()).Select(p => new[] { Math.Round(p.Lat, 5), Math.Round(p.Lng, 5) }),
                    linkReferences = service.LinkReferences.Select(p => p.ToShortRepresentation()),
                    request = coordinates,
                    service.WayPointIndices,
                    service.Timings
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
        public RequestCoordinate[] Request { get; set; }
        public RequestCoordinate[][] Requests { get; set; }
        public RequestCoordinate[] AllToAllRequests { get; set; }

        public RoutingConfig RoutingConfig { get; set; }
    }
}
