using EnergyModule.Geometry.SimpleStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Utils
{
    public class QuadTreeSearcher
    {
        public int CellCount { get; }

        private BoundingBox2D _bounds;

        public QuadCell Root { get; private set; }

        private QuadTreeSearcher(int cellCount)
        {
            CellCount = cellCount;
        }

        public static QuadTreeSearcher FromBounds(IEnumerable<IQuadTreeItem> items, int cellCount, int minSize)
        {
            var cache = new QuadTreeSearcher(cellCount);
            cache._bounds = BoundingBox2D.Empty();

            foreach (var item in items)
            {
                cache._bounds.ExtendSelf(item.Bounds);
            }

            // Get the largest dimension size (max of width and height)
            var largestDimension = Math.Max(cache._bounds.Xmax - cache._bounds.Xmin, cache._bounds.Ymax - cache._bounds.Ymin);

            // Make it recursively dividable until minSize
            var dividableSize = minSize;
            while (dividableSize < largestDimension)
                dividableSize *= cellCount;

            // Set the maxes to use this new size
            cache._bounds.Xmax = cache._bounds.Xmin + dividableSize;
            cache._bounds.Ymax = cache._bounds.Ymin + dividableSize;

            cache.Root = new QuadCell(null, (int)cache._bounds.Xmin, (int)cache._bounds.Ymin, (int)(dividableSize / cellCount), cellCount, 0, minSize, items);

            return cache;
        }

        public IEnumerable<IQuadTreeItem> Find(Point3D point)
        {
            return Find(point.X, point.Y);
        }

        public IEnumerable<IQuadTreeItem> Find(double x, double y)
        {
            if (!Root.Bounds.Contains(x, y)) yield break;
            foreach (var item in Root.Find(x, y))
                yield return item;
        }

        public (QuadCell Cell, IEnumerable<IQuadTreeItem> Items) FindCell(QuadCell previous, double x, double y, TreeNavigationStatistics stats = null)
        {
            if (previous == null) return FindCell(x, y, stats);
            return previous.FindCell(x, y, stats);
        }

        public (QuadCell Cell, IEnumerable<IQuadTreeItem> Items) FindCell(double x, double y, TreeNavigationStatistics stats = null)
        {
            return Root.FindCell(x, y, stats);
        }

        public IEnumerable<IQuadTreeItem> FindNearby(int x, int y, int searchRadius)
        {
            throw new NotImplementedException();
        }

        public void SaveAsGeoJson(string path, CoordinateConverter converter = null, Func<IQuadTreeItem, GeoJsonFeature> itemSerializer = null)
        {
            var cells = Root.RecursiveCells();

            var features = itemSerializer == null
                ? cells.Select(p => p.ToGeoJsonFeature(converter))
                : cells.SelectMany(p =>
                {
                    if (p.Items == null) return new[] { p.ToGeoJsonFeature(converter) };
                    return p.Items.Where(c => !c.FillsCell).Select(c => itemSerializer(c.Item)).Concat(new[] { p.ToGeoJsonFeature(converter) });
                });

            GeoJsonCollection
                .From(features)
                .WriteTo(path);
        }

        public (int Total, int NonEmpty, int MaxDepth) CountCells()
        {
            var total = 0;
            var nonEmpty = 0;
            var maxDepth = 0;
            foreach (var cell in Root.RecursiveCells())
            {
                total++;
                if (cell.Depth > maxDepth) maxDepth = cell.Depth;
                if (!cell.IsEmpty)
                {
                    nonEmpty++;
                }
            }

            return (total, nonEmpty, maxDepth);
        }
    }
}