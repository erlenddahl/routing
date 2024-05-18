using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Service
{
    /// <summary>
    /// Contains information about where in a returned route the parts between the requested waypoints are located.
    /// </summary>
    public class WayPointData
    {
        /// <summary>
        /// These data represent the route from the FromWaypoint, to the ToWaypoint.
        /// </summary>
        public Point3D FromWaypoint { get; set; }

        /// <summary>
        /// These data represent the route from the FromWaypoint, to the ToWaypoint.
        /// </summary>
        public Point3D ToWaypoint { get; set; }

        /// <summary>
        /// The coordinates between this waypoint and the next starts at this index in the
        /// Coordinates array.
        /// </summary>
        public int? CoordinateIndex { get; set; }

        /// <summary>
        /// The link references between this waypoint and the next starts at this index in the
        /// LinkReferences array.
        /// </summary>
        public int? LinkReferenceIndex { get; set; }

        /// <summary>
        /// Detailed information about the routing operation.
        /// </summary>
        public RoutingInfo RoutingInfo { get; set; }

        public WayPointData ConvertCoordinates(CoordinateConverter converter)
        {
            return new WayPointData()
            {
                FromWaypoint = converter.Forward(FromWaypoint),
                ToWaypoint = converter.Forward(ToWaypoint),
                CoordinateIndex = CoordinateIndex,
                LinkReferenceIndex = LinkReferenceIndex
            };
        }
    }
}