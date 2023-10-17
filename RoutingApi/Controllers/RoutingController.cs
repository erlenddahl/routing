using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Geometry;
using RoadNetworkRouting.Service;
using RoutingApi.Extensions;

namespace RoutingApi.Controllers
{
    [Route("api/[controller]")]
    [EnableCors]
    public class RoutingController : Controller
    {
        public static RoutingService Service;

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
        [ProducesResponseType(typeof(RoutingResponse), 200)]
        public ActionResult Single([FromBody] SingleRoutingRequest request)
        {
            try
            {
                this.CheckModelState();
                return Ok(request.Route(Service));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("multi")]
        [AcceptVerbs("POST")]
        [ProducesResponseType(typeof(IEnumerable<RoutingResponse>), 200)]
        public ActionResult Multiple([FromBody] MultiRoutingRequest request)
        {
            try
            {
                this.CheckModelState();
                return Ok(request.Route(Service));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("matrix")]
        [AcceptVerbs("POST")]
        [ProducesResponseType(typeof(IEnumerable<RoutingResponse>), 200)]
        public ActionResult Matrix([FromBody] MatrixRoutingRequest request)
        {
            try
            {
                this.CheckModelState();
                return Ok(request.Route(Service));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
