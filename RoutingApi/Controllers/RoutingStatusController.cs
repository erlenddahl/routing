﻿using System;
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
                LoadTimings = new {
                    LocalDijkstraRoutingService.StartedAt,
                    LocalDijkstraRoutingService.NetworkLoadedAt,
                    LocalDijkstraRoutingService.GraphCreatedAt,
                    LocalDijkstraRoutingService.NearbyLookupCreatedAt
                },
                LocalDijkstraRoutingService.GlobalTimings,
                LocalDijkstraRoutingService.TotalRequests,
                LocalDijkstraRoutingService.TotalWaypoints
            };
        }
    }
}
