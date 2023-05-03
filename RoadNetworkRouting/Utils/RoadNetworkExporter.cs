using System.Linq;
using DotSpatial.Data;
using DotSpatial.Topology;

namespace RoadNetworkRouting.Exporting
{
    public static class RoadNetworkExporter
    {
        public static void ExportNodes(this RoadNetworkRouter router, string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Point);

            var table = shp.DataTable;
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Edges", typeof(int));
            table.AcceptChanges();

            foreach (var c in router.GenerateVertices().Values)
            {
                var feature = shp.AddFeature(new Point(new Coordinate(c.X, c.Y)));
                feature.DataRow["Id"] = c.Id;
                feature.DataRow["Edges"] = c.Edges;
            }

            shp.SaveAs(shpPath, true);
        }

        public static void ExportLinks(this RoadNetworkRouter router, string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Line);

            var table = shp.DataTable;
            table.Columns.Add("Reference", typeof(string));
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("FromNode", typeof(int));
            table.Columns.Add("ToNode", typeof(int));
            table.Columns.Add("Speed", typeof(int));
            table.Columns.Add("Speed rev.", typeof(int));
            table.Columns.Add("Cost", typeof(double));
            table.Columns.Add("Cost rev.", typeof(double));
            table.AcceptChanges();

            foreach (var c in router.Links.Values)
            {
                var feature = shp.AddFeature(new LineString(c.Geometry.Select(p => new Coordinate(p.X, p.Y))));
                feature.DataRow["FromNode"] = c.FromNodeId;
                feature.DataRow["ToNode"] = c.ToNodeId;
                feature.DataRow["Id"] = c.LinkId;
                feature.DataRow["Reference"] = c.Reference;
                feature.DataRow["Cost"] = c.Cost;
                feature.DataRow["Cost rev."] = c.ReverseCost;
                feature.DataRow["Speed"] = c.SpeedLimit;
                feature.DataRow["Speed rev."] = c.SpeedLimitReversed;
            }

            shp.SaveAs(shpPath, true);
        }

        public static void ExportNodeConnections(this RoadNetworkRouter router, string shpPath)
        {
            var shp = new FeatureSet(FeatureType.Point);

            var table = shp.DataTable;
            table.Columns.Add("LinkId", typeof(int));
            table.Columns.Add("NodeId", typeof(int));
            table.Columns.Add("FromOrTo", typeof(char));
            table.Columns.Add("Distance", typeof(double));
            table.AcceptChanges();

            var vertices = router.GenerateVertices();
            foreach (var c in router.Links.Values)
            {
                var node = vertices[c.FromNodeId];
                var coordinate = c.Geometry.First();
                var feature = shp.AddFeature(new LineString(new[] { new Coordinate(node.X, node.Y), new Coordinate(coordinate.X, coordinate.Y) }));
                feature.DataRow["LinkId"] = c.LinkId;
                feature.DataRow["NodeId"] = node.Id;
                feature.DataRow["FromOrTo"] = 0;
                feature.DataRow["Distance"] = coordinate.DistanceTo2D(node.X, node.Y);

                node = vertices[c.ToNodeId];
                coordinate = c.Geometry.Last();
                feature = shp.AddFeature(new LineString(new[] { new Coordinate(node.X, node.Y), new Coordinate(coordinate.X, coordinate.Y) }));
                feature.DataRow["LinkId"] = c.LinkId;
                feature.DataRow["NodeId"] = node.Id;
                feature.DataRow["FromOrTo"] = 2;
                feature.DataRow["Distance"] = coordinate.DistanceTo2D(node.X, node.Y);
            }

            shp.SaveAs(shpPath, true);
        }
    }
}