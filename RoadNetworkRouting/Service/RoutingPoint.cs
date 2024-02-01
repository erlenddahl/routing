using System.Diagnostics;
using System.IO;
using System.Reflection;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.Utilities.Caching;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Service
{
    public class RoutingPoint
    {
        /// <summary>
        /// The original search point.
        /// </summary>
        public Point3D SearchPoint { get; }

        /// <summary>
        ///  The link that is the nearest link to the <see cref="SearchPoint"/>.
        /// </summary>
        public RoadLink Link { get; set; }

        /// <summary>
        /// The point on the <see cref="Link"/> that is the nearest point ot the <see cref="SearchPoint"/>.
        /// </summary>
        public NearestPointInfo Nearest { get; set; }

        public RoutingPoint(Point3D point)
        {
            SearchPoint = point;
        }

        public RoutingPoint(Point3D point, RoadLink roadLink, NearestPointInfo nearest)
        {
            SearchPoint = point;
            Link = roadLink;
            Nearest = nearest;
        }

        public void Update(RoutingPoint point)
        {
            Link = point.Link;
            Nearest = point.Nearest;
        }
    }
}