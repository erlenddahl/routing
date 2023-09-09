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
                FullRoutingService.StartedAt,
                LoadTimings = FullRoutingService.Timings.GetTimingsInMs(),
                FullRoutingService.GlobalTimings,
                FullRoutingService.TotalRequests,
                FullRoutingService.TotalWaypoints
            };
        }
    }
}
