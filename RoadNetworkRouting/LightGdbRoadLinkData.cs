using System.Linq;
using no.sintef.SpeedModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting
{
    public class LightGdbRoadLinkData
    {
        public string Reference { get; set; }
        public bool? FromNodeLocked { get; set; }
        public int FromNodeId { get; set; }
        public int FromNodeConnectionTolerance { get; set; }
        public bool? ToNodeLocked { get; set; }
        public int ToNodeId { get; set; }
        public int ToNodeConnectionTolerance { get; set; }
        public Point3D FirstPoint { get; set; }
        public Point3D LastPoint { get; set; }

        public LightGdbRoadLinkData(GdbRoadLinkData link)
        {
            FirstPoint = link.Geometry.Points.First();
            LastPoint = link.Geometry.Points.Last();
        }

        public LightGdbRoadLinkData()
        {

        }
    }
}