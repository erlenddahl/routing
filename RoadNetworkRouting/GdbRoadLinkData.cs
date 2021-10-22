using no.sintef.SpeedModule.Geometry;

namespace RoadNetworkRouting
{
    public class GdbRoadLinkData
    {
        public string Reference { get; set; }
        public int RoadClass { get; set; }
        public int LinkId { get; set; }
        public double FromRelativeLength { get; set; }
        public double ToRelativeLength { get; set; }
        public int FromNodeId { get; set; }
        public int ToNodeId { get; set; }
        public int RoadNumber { get; set; }
        public string Direction { get; set; }
        public int SpeedLimit { get; set; }
        public int SpeedLimitReversed { get; set; }
        public string RoadType { get; set; }
        public PolyLineZ Geometry { get; set; }
        public string SpecialRoad { get; set; }
        public string LaneCode { get; set; }
        public double Cost { get; set; }
        public double ReverseCost { get; set; }
        public int FromNodeConnectionTolerance { get; set; }
        public int ToNodeConnectionTolerance { get; set; }
    }
}