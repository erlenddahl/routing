using System.Collections.Generic;
using System.Linq;
using ConsoleUtilities.ConsoleInfoPanel;
using Gdb2Xml;
using no.sintef.SpeedModule.Geometry;
using RoadNetworkRouting;
using Routing;

namespace NetworkGenerator
{
    public class GdbGraphBuilder
    {
        public static (Graph, GdbRoadLinkData[]) Create(string path, ConsoleInformationPanel cip)
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
        }

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

        public static IEnumerable<LightGdbRoadLinkData> ProcessTableLight(string dbPath)
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
        }
    }
}