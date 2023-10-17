using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoadNetworkRouting.Service;

namespace RoutingApi.Controllers
{
    [Route("api/[controller]")]
    [EnableCors]
    public class RoutingStatusController : Controller
    {
        [HttpGet]
        public object Get()
        {
            return new
            {
                Api = "Routing",
                Version = "3",
                RoutingController.Service.StartedAt,
                LoadTimings = RoutingController.Service.Timings.GetTimingsInMs(),
                RoutingController.Service.GlobalTimings,
                RoutingController.Service.TotalRequests,
                RoutingController.Service.TotalWaypoints
            };
        }
    }
}
