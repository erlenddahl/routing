using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RoutingApi.Geometry;
using RoutingApi.Helpers;

namespace RoutingApi.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    public class RoutingController : Controller
    {
        // GET api/values
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public object Get()
        {
            return new
            {
                Api = "Routing",
                Version = "2"
            };
        }

        // POST api/values
        [Microsoft.AspNetCore.Mvc.HttpPost]
        [EnableCors("AllowAll")]
        [Microsoft.AspNetCore.Mvc.Route("")]
        [Microsoft.AspNetCore.Mvc.Route("GetRoute")]
        [Microsoft.AspNetCore.Mvc.AcceptVerbs("POST")]
        public object FindRoute([Microsoft.AspNetCore.Mvc.FromBody] List<LatLng> coordinates)
        {
            RoutingService service = null;
            try
            {
                service = RoutingService.FromLatLng(coordinates);

                return new
                {
                    distance = PointUtm33.Distance(service.Route),
                    coordinates = service.Route.Select(p => p.ToWgs84()).Select(p => new[] {Math.Round(p.Lat, 5), Math.Round(p.Lng, 5)}),
                    linkReferences = service.LinkReferences.Select(p => new {S = p.ToShortRepresentation()}),
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
