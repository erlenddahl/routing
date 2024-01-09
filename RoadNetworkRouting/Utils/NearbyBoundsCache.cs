using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.GeoJson;

namespace RoadNetworkRouting.Utils
{
    public class NearbyBoundsCache<T>
    {
        /// <summary>
        /// A lookup that can be used to find the cell position in the _grid list.
        /// </summary>
        private readonly Dictionary<string, (int X, int Y)> _lookup = new();

        /// <summary>
        /// A 2D list that contains a compressed (no holes) representation of the
        /// cache cells.
        /// Each row contains a list of all columns in this row, and each column contains
        /// a list of all items in this cell.
        /// </summary>
        private List<GridRow> _grid = new();

        private static BoundingBox2D _bounds;

        /// <summary>
        /// The size (width and height) of each cells in this boundary lookup.
        /// </summary>
        public int CellSize { get; }

        public int ItemCount => _grid.Sum(p => p.Columns.Sum(c => c.Items.Count));

        private NearbyBoundsCache(int cellSize)
        {
            CellSize = cellSize;
        }

        public static NearbyBoundsCache<T> FromBounds(IEnumerable<T> items, Func<T, BoundingBox2D> bounds, int cellSize)
        {
            var cache = new NearbyBoundsCache<T>(cellSize);
            var lookup = new Dictionary<(int X, int Y), List<GridCell>>();
            _bounds = BoundingBox2D.Empty();

            foreach (var item in items)
            {
                var b = bounds(item);

                var (minX, minY) = cache.GetCellCoordinates(b.Xmin, b.Ymin);
                var (maxX, maxY) = cache.GetCellCoordinates(b.Xmax, b.Ymax);
                
                // If the bounds are exactly on the borders between cells, extend them
                // so that the neighboring cells also contain these items.
                if (b.Xmin - minX == 0) minX -= cellSize;
                if (b.Ymin - minY == 0) minY -= cellSize;
                if (b.Xmax - maxX == 0) maxX += cellSize;
                if (b.Ymax - maxY == 0) maxY += cellSize;

                _bounds.ExtendSelf(b);

                for (var x = minX; x <= maxX; x += cellSize)
                for (var y = minY; y <= maxY; y += cellSize)
                {
                    var key = (x, y);
                    if (lookup.TryGetValue(key, out var list))
                        list.Add(new GridCell(item, b));
                    else
                        lookup.Add(key, new List<GridCell>() { new(item, b) });
                }
            }

            var rows = lookup.GroupBy(p => p.Key.Y).OrderBy(p => p.Key);
            foreach (var row in rows)
            {
                var columns= row
                    .Select(p => (p.Key.X, p.Value))
                    .OrderBy(p => p.X)
                    .ToList();

                cache._grid.Add(new GridRow(row.Key, cellSize, columns));

                // Create a lookup from the nearby key to each cell's position
                // within the _grid.
                for (var i = 0; i < columns.Count; i++)
                {
                    cache._lookup.Add(columns[i].X + "_" + row.Key, (i, cache._grid.Count - 1));
                }
            }

            return cache;
        }

        private string GetNearbyKey(Point3D point)
        {
            return GetNearbyKey(point.X, point.Y);
        }

        private (int, int) GetCellCoordinates(double x, double y)
        {
            var ix = (int)(x / CellSize) * CellSize;
            var iy = (int)(y / CellSize) * CellSize;
            if (x < 0) ix -= CellSize;
            if (y < 0) iy -= CellSize;
            return (ix, iy);
        }

        private string GetNearbyKey(double x, double y)
        {
            var (ix, iy) = GetCellCoordinates(x, y);
            return ix + "_" + iy;
        }

        public IEnumerable<T> GetNearbyItems(Point3D point, int searchRadius = 0)
        {
            return GetNearbyItems(point.X, point.Y, searchRadius);
        }

        public IEnumerable<T> GetNearbyItems(double x, double y, int searchRadius = 0)
        {
            HashSet<BoundingBox2D> returned = new();
            var r = (int)Math.Ceiling(searchRadius / (double)CellSize) * CellSize;

            foreach (var row in GetNearby(_grid, y, searchRadius))
            foreach (var col in GetNearby(row.Columns, x, searchRadius))
            foreach (var item in col.Items)
                if (!returned.Contains(item.Bounds))
                    if (item.Bounds.Contains(x, y, searchRadius))
                    {
                        yield return item.Item;
                        returned.Add(item.Bounds);
                    }
        }

        private IEnumerable<T> GetNearby<T>(IList<T> items, double value, double radius) where T : IsWithinnable
        {
            var ix = items.Count / 2;
            var binaryWidth = ix / 2;
            var maxSearches = (int)Math.Ceiling(Math.Log2(items.Count));
            var searches = 0;
            while (true)
            {
                if (searches > maxSearches + 1 || ix < 0 || ix >= items.Count)
                    yield break;
                if (items[ix].IsAfter(value, radius))
                    ix -= binaryWidth;
                else if (items[ix].IsBefore(value, radius))
                    ix += binaryWidth;
                else
                    break;
                binaryWidth = Math.Max(1, binaryWidth / 2);
                searches++;
            }

            for (var i = ix; i < items.Count; i++)
            {
                if (items[i].IsWithin(value, radius)) yield return items[i];
                else break;
            }

            for (var i = ix - 1; i >= 0; i--)
            {
                if (items[i].IsWithin(value, radius)) yield return items[i];
                else break;
            }
        }

        public IEnumerable<T> GetItemsInCell(Point3D point)
        {
            return GetNearbyItems(point.X, point.Y);
        }

        public IEnumerable<T> GetItemsInCell(double x, double y)
        {
            var key = GetNearbyKey(x, y);
            if (_lookup.TryGetValue(key, out var g))
                foreach (var item in _grid[g.Y].Columns[g.X].Items)
                    yield return item.Item;
        }

        public void WriteGeoJson(string path)
        {
            var features = new List<GeoJsonFeature>();
            for (var x = _bounds.Xmin + 1; x <= _bounds.Xmax; x += CellSize)
            for (var y = _bounds.Ymin + 1; y <= _bounds.Ymax; y += CellSize)
            {
                var itemCount = GetItemsInCell(x, y).Count();
                if (itemCount <= 0) continue;
                features.Add(GeoJsonFeature.Polygon(new BoundingBox2D(x - 1, x - 1 + CellSize, y - 1, y - 1 + CellSize).Corners, new
                {
                    polygons = itemCount
                }));
            }

            GeoJsonCollection.From(features).WriteTo(path);
        }

        private interface IsWithinnable
        {
            public bool IsWithin(double value, double radius);
            public bool IsBefore(double value, double radius);
            public bool IsAfter(double value, double radius);
        }

        private class GridRow : IsWithinnable
        {
            private readonly BoundingBox2D _bounds;
            public int MinY { get; set; }
            public int MaxY { get; set; }
            public List<GridColumn> Columns { get; set; }

            public GridRow(int minY, int cellSize, List<(int X, List<GridCell> Items)> columns)
            {
                MinY = minY;
                MaxY = minY + cellSize;
                _bounds = new BoundingBox2D(-1, 1, MinY, MaxY);
                Columns = columns.Select(p => new GridColumn(p.X, cellSize, p.Items)).ToList();
            }

            public bool IsWithin(double y, double radius)
            {
                return _bounds.Contains(0, y, (int)radius);
            }

            /// <summary>
            /// Returns true if this item is sorted before an item that would return
            /// true on IsWithin in a list.
            /// </summary>
            /// <param name="y"></param>
            /// <param name="radius"></param>
            /// <returns></returns>
            public bool IsBefore(double y, double radius)
            {
                return MaxY + radius < y;
            }

            /// <summary>
            /// Returns true if this item is sorted after an item that would return
            /// true on IsWithin in a list.
            /// </summary>
            /// <param name="y"></param>
            /// <param name="radius"></param>
            /// <returns></returns>
            public bool IsAfter(double y, double radius)
            {
                return MinY - radius > y;
            }

            public override string ToString()
            {
                return $"Y {MinY} to {MaxY}, {Columns.Count} columns";
            }
        }

        private class GridColumn : IsWithinnable
        {
            private readonly BoundingBox2D _bounds;
            public int MinX { get; set; }
            public int MaxX { get; set; }
            public List<GridCell> Items { get; set; }

            public GridColumn(int minX, int cellSize, List<GridCell> items)
            {
                MinX = minX;
                MaxX = minX + cellSize;
                _bounds = new BoundingBox2D(MinX, MaxX, -1, 1);
                Items = items;
            }

            public bool IsWithin(double x, double radius)
            {
                return _bounds.Contains(x, 0, (int)radius);
            }

            public override string ToString()
            {
                return $"X {MinX} to {MaxX}, {Items.Count} items";
            }

            /// <summary>
            /// Returns true if this item is sorted before an item that would return
            /// true on IsWithin in a list.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="radius"></param>
            /// <returns></returns>
            public bool IsBefore(double x, double radius)
            {
                return MaxX + radius < x;
            }

            /// <summary>
            /// Returns true if this item is sorted after an item that would return
            /// true on IsWithin in a list.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="radius"></param>
            /// <returns></returns>
            public bool IsAfter(double x, double radius)
            {
                return MinX - radius > x;
            }
        }

        private class GridCell
        {
            public T Item { get; set; }
            public BoundingBox2D Bounds { get; set; }
            public GridCell(T item, BoundingBox2D bounds)
            {
                Item = item;
                Bounds = bounds;
            }

            public override string ToString()
            {
                return Bounds.ToString();
            }
        }
    }
}
