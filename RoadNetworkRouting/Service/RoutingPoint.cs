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
        public Point3D Point { get; }
        public RoadLink Link { get; set; }
        public NearestPointInfo Nearest { get; set; }

        public RoutingPoint(Point3D point)
        {
            Point = point;
        }

        public RoutingPoint(Point3D point, RoadLink roadLink, NearestPointInfo nearest)
        {
            Point = point;
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