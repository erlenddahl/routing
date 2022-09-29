using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleUtilities.ConsoleInfoPanel;
using EnergyModule.Geometry;
using Gdb2Xml;

namespace NetworkGenerator
{
    public class GdbGraphBuilder
    {
        /*public static (Graph, GdbRoadLinkData[]) Create(string path, ConsoleInformationPanel cip)
        {
            var links = ProcessTable(path).ToArray();
            return (Graph.Create(links.Select(p => new GraphDataItem()
            {
                Cost = p.Cost,
                EdgeId = p.LinkId,
                ReverseCost = p.ReverseCost,
                Id = p.Reference,
                SourceVertexId = p.FromNodeId,
                TargetVertexId = p.ToNodeId
            })), links);
        }*/

        public static IEnumerable<GdbRoadLinkData> ProcessTable(string dbPath)
        {
            var ix = 0;
            foreach (var row in GeodatabaseCache.OpenAndReadTable(dbPath, new TableInfo("ERFKPS", true, "*")))
            {
                yield return new GdbRoadLinkData()
                {
                    Reference = row.GetDouble("FROM_M") + "-" + row.GetDouble("TO_M") + "@" + row.GetString("ROUTEID"),
                    RoadClass = row.GetInteger("RoadClass"),
                    LinkId = ix++,
                    FromRelativeLength = row.GetDouble("FROM_M"),
                    ToRelativeLength = row.GetDouble("TO_M"),
                    FromNodeId = row.GetString("FromNodeID") == "" ? -1 : int.Parse(row.GetString("FromNodeID")),
                    ToNodeId = row.GetString("ToNodeID") == "" ? -1 : int.Parse(row.GetString("ToNodeID")),
                    Cost = row.GetDouble("FT_MINUTES") * 60,
                    ReverseCost = row.GetDouble("TF_MINUTES") * 60,
                    RoadNumber = row.GetInteger("VEGNUMMER"),
                    Direction = row.GetString("ONEWAY"),
                    SpeedLimit = row.GetInteger("FT_Fart"),
                    SpeedLimitReversed = row.GetInteger("TF_Fart"),
                    RoadType = row.GetString("VEGTYPE"),
                    Geometry = PolyLineZ.ParseEsri(row.GetGeometry().shapeBuffer, true),
                    SpecialRoad = row.IsNull("SPECIALVEG") ? null : row.GetString("SPECIALVEG")
                };
            }
        }

        public static IEnumerable<GdbRoadLinkData> ProcessNewTable(string dbPath)
        {
            var ix = 0;
            foreach (var row in GeodatabaseCache.OpenAndReadTable(dbPath, new TableInfo("ruttger_link_geom", true, "*")))
            {
                yield return new GdbRoadLinkData()
                {
                    Reference = row.GetDouble("from_measure") + "-" + row.GetDouble("to_measure") + "@" + row.GetString("routeid"),
                    RoadClass = row.GetInteger("roadclass"),
                    LinkId = ix++,
                    FromRelativeLength = row.GetDouble("from_measure"),
                    ToRelativeLength = row.GetDouble("to_measure"),
                    FromNodeId = row.GetString("fromnode") == "" ? -1 : int.Parse(row.GetString("fromnode")),
                    ToNodeId = row.GetString("tonode") == "" ? -1 : int.Parse(row.GetString("tonode")),
                    Cost = row.GetDouble("drivetime_fw") * 60,
                    ReverseCost = row.GetDouble("drivetime_bw") * 60,
                    RoadNumber = row.GetInteger("roadnumber"),
                    Direction = row.GetString("oneway"),
                    SpeedLimit = row.GetInteger("speedfw"),
                    SpeedLimitReversed = row.GetInteger("speedbw"),
                    //RoadType = row.GetString("VEGTYPE"),
                    Geometry = PolyLineZ.ParseEsri(row.GetGeometry().shapeBuffer, true)
                };
            }
        }

        /*public static IEnumerable<LightGdbRoadLinkData> ProcessTableLight(string dbPath)
        {
            var ix = 0;
            foreach (var row in GeodatabaseCache.OpenAndReadTable(dbPath, new TableInfo("ERFKPS", true, "*")))
            {
                var geometry = PolyLineZ.ParseEsri(row.GetGeometry().shapeBuffer, true);
                yield return new LightGdbRoadLinkData()
                {
                    Reference = row.GetDouble("FROM_M") + "-" + row.GetDouble("TO_M") + "@" + row.GetString("ROUTEID"),
                    FromNodeId = row.GetString("FromNodeID") == "" ? -1 : int.Parse(row.GetString("FromNodeID")),
                    ToNodeId = row.GetString("ToNodeID") == "" ? -1 : int.Parse(row.GetString("ToNodeID")),
                    FirstPoint = geometry.Points.First(),
                    LastPoint = geometry.Points.Last()
                };
            }
        }*/
    }

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