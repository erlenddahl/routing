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
        /// Short information about the source of the data and algorithms that provided this route.
        /// </summary>
        public RoutingSourceInfo RoutingSource { get; set; }

        /// <summary>
        /// Contains a full list of the link references of all links in the route.
        /// </summary>
        public List<string> LinkReferences { get; set; }

        /// <summary>
        /// Contains a full list of the link data for all links in the route.
        /// </summary>
        public List<GeometryLink> Links { get; set; }

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

    public class RoutingSourceInfo
    {
        public string Router { get; set; }
        public string Network { get; set; }

        public RoutingSourceInfo(string router, string network = null)
        {
            Router = router;
            Network = network;
        }
    }
}