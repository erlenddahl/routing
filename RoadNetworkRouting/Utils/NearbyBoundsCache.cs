using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Utils
{
    public class NearbyBoundsCache<T>
    {
        private readonly Dictionary<string, List<(T Item, BoundingBox2D Bounds)>> _nearbyLookup = new();
        public int CellSize { get; }

        private NearbyBoundsCache(int cellSize)
        {
            CellSize = cellSize;
        }

        public static NearbyBoundsCache<T> FromBounds(IEnumerable<T> items, Func<T, BoundingBox2D> bounds, int cellSize)
        {
            var cache = new NearbyBoundsCache<T>(cellSize);

            foreach (var item in items)
            {
                var b = bounds(item);

                var minX = (int)(b.Xmin / cellSize) * cellSize;
                if (b.Xmin - minX == 0) minX -= cellSize;

                var maxX = (int)(b.Xmax / cellSize) * cellSize;
                if (b.Xmax - maxX == 0) maxX += cellSize;

                var minY = (int)(b.Ymin / cellSize) * cellSize;
                if (b.Ymin - minY == 0) minY -= cellSize;

                var maxY = (int)(b.Ymax / cellSize) * cellSize;
                if (b.Ymax - maxY == 0) maxY += cellSize;

                for (var x = minX; x <= maxX; x += cellSize)
                for (var y = minY; y <= maxY; y += cellSize)
                {
                    var key = cache.GetNearbyKey(x, y);
                    if (cache._nearbyLookup.TryGetValue(key, out var list))
                        list.Add((item, b));
                    else
                        cache._nearbyLookup.Add(key, new List<(T, BoundingBox2D)>() { (item, b) });
                }
            }

            return cache;
        }

        public string GetNearbyKey(Point3D point)
        {
            return GetNearbyKey(point.X, point.Y);
        }

        public string GetNearbyKey(double x, double y)
        {
            return ((int)(x / CellSize) * CellSize) + "_" + ((int)(y / CellSize) * CellSize);
        }

        public IEnumerable<T> GetNearbyItems(Point3D point, int searchRadius = 0)
        {
            return GetNearbyItems(point.X, point.Y, searchRadius);
        }

        public IEnumerable<T> GetNearbyItems(double x, double y, int searchRadius = 0)
        {
            HashSet<BoundingBox2D> returned = new();
            var r = (int)Math.Ceiling(searchRadius / (double)CellSize) * CellSize;
            for (var xi = x - r; xi <= x + r; xi += CellSize)
            for (var yi = y - r; yi <= y + r; yi += CellSize)
            {
                var key = GetNearbyKey(x, y);
                if (_nearbyLookup.TryGetValue(key, out var items))
                    foreach (var item in items)
                        if (!returned.Contains(item.Bounds))
                            if (item.Bounds.Contains(x, y, searchRadius))
                            {
                                yield return item.Item;
                                returned.Add(item.Bounds);
                            }
            }
        }

        public IEnumerable<T> GetItemsInCell(Point3D point)
        {
            return GetNearbyItems(point.X, point.Y);
        }

        public IEnumerable<T> GetItemsInCell(double x, double y)
        {
            var key = GetNearbyKey(x, y);
            if (_nearbyLookup.TryGetValue(key, out var items))
                foreach (var item in items)
                    yield return item.Item;
        }
    }
}
