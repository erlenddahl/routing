using EnergyModule.Geometry.SimpleStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Utils
{
    public interface IQuadTreeItem
    {
        BoundingBox2D Bounds { get; }

        bool Overlaps(BoundingBox2D bounds);
        bool Contains(double x, double y);
    }

    public class SimplerQuadTreeSearcher
    {
        public int CellCount { get; }

        private BoundingBox2D _bounds;

        private QuadCell _root;

        private SimplerQuadTreeSearcher(int cellCount)
        {
            CellCount = cellCount;
        }

        public static SimplerQuadTreeSearcher FromBounds(IEnumerable<IQuadTreeItem> items, int cellCount, int minSize)
        {
            var cache = new SimplerQuadTreeSearcher(cellCount);
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

            cache._root = new QuadCell((int)cache._bounds.Xmin, (int)cache._bounds.Ymin, (int)(dividableSize / cellCount), cellCount, 0, minSize, items);

            return cache;
        }

        public IEnumerable<IQuadTreeItem> Find(Point3D point)
        {
            return Find(point.X, point.Y);
        }

        public IEnumerable<IQuadTreeItem> Find(double x, double y)
        {
            if (!_root.Bounds.Contains(x, y)) yield break;
            foreach (var item in _root.Find(x, y))
                yield return item;
        }

        public IEnumerable<IQuadTreeItem> FindNearby(int x, int y, int searchRadius)
        {
            throw new NotImplementedException();
        }

        public void SaveAsGeoJson(string path, CoordinateConverter converter = null)
        {
            GeoJsonCollection
                .From(_root.RecursiveCells().Select(p => p.ToGeoJsonFeature(converter)))
                .WriteTo(path);
        }

        public (int Total, int NonEmpty, int MaxDepth) CountCells()
        {
            var total = 0;
            var nonEmpty = 0;
            var maxDepth = 0;
            foreach (var cell in _root.RecursiveCells())
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

    public class QuadCell
    {
        private readonly int _cellSize;
        public readonly int Depth;
        private readonly int _totalChildren;
        public BoundingBox2D Bounds { get; set; }
        public QuadCell[,] Children { get; set; }

        public IQuadTreeItem[] Items { get; set; }
        public bool IsEmpty => Items != null && !Items.Any();

        public QuadCell(int xMin, int yMin, int cellSize, int cellCount, int depth, int minSize, IEnumerable<IQuadTreeItem> items)
        {
            Bounds = new BoundingBox2D(xMin, xMin + cellSize * cellCount, yMin, yMin + cellSize * cellCount);
            _cellSize = cellSize;
            Depth = depth;
            _totalChildren = 0;

            // Calculate the size of the next recursion
            var childSize = cellSize / cellCount;

            // If there are no children, or if this cell is too small,
            // we store all children directly, and exit here.
            if (!items.Any() || childSize < minSize)
            {
                Items = items.ToArray();
                return;
            }

            // Otherwise, we need to generate the child cells.

            // Prepare a 2D grid with bounds and child lists to keep track of the children for each cell.
            var bounds = new BoundingBox2D[cellCount, cellCount];
            var children = new List<IQuadTreeItem>[cellCount, cellCount];
            for (var x = 0; x < cellCount; x++)
            for (var y = 0; y < cellCount; y++)
            {
                var sx = xMin + x * cellSize;
                var sy = yMin + y * cellSize;
                bounds[x, y] = new BoundingBox2D(sx, sx + cellSize, sy, sy + cellSize);
                children[x, y] = new List<IQuadTreeItem>();
            }

            // Go through all items and put them in the matching child list(s).
            foreach (var item in items)
            {
                _totalChildren++;
                var added = false;

                for (var x = 0; x < cellCount; x++)
                for (var y = 0; y < cellCount; y++)
                {
                    // If the bounds for this cell overlap with the bounds
                    // for this child, put it in the corresponding cell.
                    if (item.Overlaps(bounds[x, y]))
                    {
                        children[x, y].Add(item);
                        added = true;
                    }
                }

                if (!added)
                {
                    throw new Exception("Child with bounds [" + item.Bounds + "] did not match any of the children of the cell with bounds [" + Bounds + "].");
                }
            }

            // If all the given items completely contain this cell, we don't need to recurse any more,
            // since all descendants of this cell will contain all the same items.
            if (Enumerate(children).All(p => p.Count == _totalChildren))
            {
                Items = items.ToArray();
                return;
            }

            // Generate the child cells using the already created bounds and child lists.
            Children = new QuadCell[cellCount, cellCount];
            for (var x = 0; x < cellCount; x++)
            for (var y = 0; y < cellCount; y++)
            {
                Children[x, y] = new QuadCell((int)bounds[x, y].Xmin, (int)bounds[x, y].Ymin, childSize, cellCount, depth + 1, minSize, children[x, y]);
            }
        }

        public IEnumerable<IQuadTreeItem> Find(double x, double y)
        {
            if (Items != null)
                return Items.Where(p => p.Contains(x, y)).Select(p => p);
            return GetChild(x, y)?.Find(x, y) ?? Array.Empty<IQuadTreeItem>();
        }

        private QuadCell GetChild(double x, double y)
        {
            x -= Bounds.Xmin;
            y -= Bounds.Ymin;
            x /= _cellSize;
            y /= _cellSize;

            return Children[(int)x, (int)y];
        }

        public override string ToString()
        {
            return Bounds + (Items?.Any() == true ? ", " + Items.Length + " items" : "");
        }

        public IEnumerable<QuadCell> Cells()
        {
            return Enumerate(Children);
        }

        private static IEnumerable<TC> Enumerate<TC>(TC[,] cells)
        {
            if (cells == null) yield break;
            for (var x = 0; x < cells.GetLength(0); x++)
            for (var y = 0; y < cells.GetLength(1); y++)
                if (cells[x, y] != null)
                    yield return cells[x, y];
        }

        public IEnumerable<QuadCell> RecursiveCells()
        {
            foreach (var cell in Cells()) yield return cell;

            foreach (var cell in Cells())
            foreach (var child in cell.RecursiveCells())
                yield return child;
        }

        public GeoJsonFeature ToGeoJsonFeature(CoordinateConverter converter = null)
        {
            var props = new
            {
                size = _cellSize,
                depth = Depth,
                totalDescendants = _totalChildren,
                directChildItems = Items?.Length ?? 0,
                populatedChildCells = Cells().Count()
            };

            return converter != null
                ? GeoJsonFeature.Polygon(Bounds.Corners, converter, props)
                : GeoJsonFeature.Polygon(Bounds.Corners, props);
        }
    }
}