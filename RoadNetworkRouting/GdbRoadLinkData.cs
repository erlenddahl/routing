using System;
using System.Collections.Generic;
using EnergyModule.Geometry;
using Newtonsoft.Json.Linq;

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
        public string RoadType { get; set; } = "";
        public PolyLineZ Geometry { get; set; }
        public string SpecialRoad { get; set; } = "";
        public string LaneCode { get; set; } = "";
        public double Cost { get; set; }
        public double ReverseCost { get; set; }
        public int FromNodeConnectionTolerance { get; set; }
        public int ToNodeConnectionTolerance { get; set; }
        public object Raw { get; set; }

        public GdbRoadLinkData Clone(PolyLineZ newGeometry = null)
        {
            return new GdbRoadLinkData()
            {
                Reference = Reference,
                RoadClass = RoadClass,
                LinkId = LinkId,
                FromRelativeLength = FromRelativeLength,
                ToRelativeLength = ToRelativeLength,
                FromNodeId = FromNodeId,
                ToNodeId = ToNodeId,
                RoadNumber = RoadNumber,
                Direction = Direction,
                SpeedLimit = SpeedLimit,
                SpeedLimitReversed = SpeedLimitReversed,
                RoadType = RoadType,
                Geometry = newGeometry ?? Geometry,
                SpecialRoad = SpecialRoad,
                LaneCode = LaneCode,
                Cost = Cost,
                ReverseCost = ReverseCost,
                FromNodeConnectionTolerance = FromNodeConnectionTolerance,
                ToNodeConnectionTolerance = ToNodeConnectionTolerance,
                Raw = Raw
            };
        }

        public override string ToString()
        {
            var c = Math.Abs(Cost - double.MaxValue) < 0.000001 ? "INF" : Cost.ToString("n2");
            var rc = Math.Abs(ReverseCost - double.MaxValue) < 0.000001 ? "INF" : ReverseCost.ToString("n2");
            return $"Id={LinkId}, Cost={c} / {rc}";
        }
    }
}