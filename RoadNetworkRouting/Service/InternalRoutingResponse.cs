using System.Collections.Generic;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using Extensions.Utilities;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Service
{
    public class InternalRoutingResponse
    {
        /// <summary>
        /// Contains a full list of the link references of all links in the route.
        /// </summary>
        public List<string> LinkReferences { get; set; }

        /// <summary>
        /// Contains a full list of the link data for all links in the route.
        /// </summary>
        public List<RoadLink> Links { get; set; }

        /// <summary>
        /// Contains detailed 3D coordinates for the entire route.
        /// </summary>
        public List<Point3D> Coordinates { get; set; }

        /// <summary>
        /// Contains information that can be used to map the requested waypoints with the returned LinkReferences and and Coordinates arrays
        /// </summary>
        public List<WayPointData> RequestedWaypoints { get; set; }

        /// <summary>
        /// Contains timing data on the routing.
        /// </summary>
        public TaskTimer Timings { get; set; }
    }
}