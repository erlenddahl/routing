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
    public interface IQuadTreeItem
    {
        BoundingBox2D Bounds { get; }

        bool Overlaps(BoundingBox2D bounds);
        bool Contains(double x, double y);
        bool ContainsEntireCell(BoundingBox2D bounds);
        int GetEdgeCount();
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

            cache._root = new QuadCell(null, (int)cache._bounds.Xmin, (int)cache._bounds.Ymin, (int)(dividableSize / cellCount), cellCount, 0, minSize, items);

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

        public (QuadCell Cell, IEnumerable<IQuadTreeItem> Items) FindCell(QuadCell previous, double x, double y, TreeNavigationStatistics stats = null)
        {
            if (previous == null) return FindCell(x, y, stats);
            return previous.FindCell(x, y, stats);
        }

        public (QuadCell Cell, IEnumerable<IQuadTreeItem> Items) FindCell(double x, double y, TreeNavigationStatistics stats = null)
        {
            return _root.FindCell(x, y, stats);
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

    public class TreeNavigationStatistics
    {
        public int ZoomedIn { get; set; }
        public int ZoomedOut { get; set; }
        public int BoundaryChecks { get; set; }
        public int BoundaryLowLevelChecks { get; set; }
        public int LeavesReturned { get; set; }
        public int SimpleLeaves { get; set; }
        public int HardLeaves { get; set; }
        public int CompletelyOutside { get; set; }
        public int FillsCellChecks { get; set; }
        public int PolygonsReturned { get; set; }
        public int FullPolygonChecks { get; set; }
        public int EdgesChecked { get; set; }

        public int Operations => BoundaryLowLevelChecks + ZoomedIn * 4 + ZoomedOut + EdgesChecked * 8;

        public void IncrementZoomIns()
        {
            ZoomedIn++;
        }

        public void IncrementZoomOuts()
        {
            ZoomedOut++;
        }

        public void IncrementBoundaryChecks(int lowLevelCount)
        {
            BoundaryChecks++;
            BoundaryLowLevelChecks += lowLevelCount;
        }

        public void IncrementLeafReturns(int itemCount)
        {
            LeavesReturned++;
            if (itemCount < 1) SimpleLeaves++;
            else HardLeaves++;
        }

        public void IncrementOutside()
        {
            CompletelyOutside++;
        }

        public void IncreaseFillsCellCheck()
        {
            FillsCellChecks++;
        }

        public void IncreaseReturnedPolygon()
        {
            PolygonsReturned++;
        }

        public void IncreaseFullPolygonCheck(int edgeCount)
        {
            FullPolygonChecks++;
            EdgesChecked += edgeCount;
        }

        public TreeNavigationStatistics Diff(TreeNavigationStatistics other)
        {
            return new TreeNavigationStatistics()
            {
                ZoomedOut = ZoomedOut - other.ZoomedOut,
                ZoomedIn = ZoomedIn - other.ZoomedIn,
                SimpleLeaves = SimpleLeaves - other.SimpleLeaves,
                EdgesChecked = EdgesChecked - other.EdgesChecked,
                BoundaryChecks = BoundaryChecks - other.BoundaryChecks,
                CompletelyOutside = CompletelyOutside - other.CompletelyOutside,
                FillsCellChecks = FillsCellChecks - other.FillsCellChecks,
                FullPolygonChecks = FullPolygonChecks - other.FullPolygonChecks,
                HardLeaves = HardLeaves - other.HardLeaves,
                LeavesReturned = LeavesReturned - other.LeavesReturned,
                PolygonsReturned = PolygonsReturned - other.PolygonsReturned,
                BoundaryLowLevelChecks = BoundaryLowLevelChecks - other.BoundaryLowLevelChecks
            };
        }

        public TreeNavigationStatistics Clone()
        {
            return new TreeNavigationStatistics()
            {
                ZoomedOut = ZoomedOut,
                ZoomedIn = ZoomedIn,
                SimpleLeaves = SimpleLeaves,
                EdgesChecked = EdgesChecked,
                BoundaryChecks = BoundaryChecks,
                CompletelyOutside = CompletelyOutside,
                FillsCellChecks = FillsCellChecks,
                FullPolygonChecks = FullPolygonChecks,
                HardLeaves = HardLeaves,
                LeavesReturned = LeavesReturned,
                PolygonsReturned = PolygonsReturned,
                BoundaryLowLevelChecks = BoundaryLowLevelChecks
            };
        }
    }

    public class QuadCell
    {
        private readonly int _cellSize;
        public readonly int Depth;
        private readonly int _totalChildren;
        public BoundingBox2D Bounds { get; set; }
        public QuadCell[,] Children { get; set; }
        public QuadCell Parent { get; set; }

        public (bool FillsCell, IQuadTreeItem Item)[] Items { get; set; }
        public bool IsEmpty => Items != null && !Items.Any();

        public QuadCell(QuadCell parent, int xMin, int yMin, int cellSize, int cellCount, int depth, int minSize, IEnumerable<IQuadTreeItem> items)
        {
            Bounds = new BoundingBox2D(xMin, xMin + cellSize * cellCount, yMin, yMin + cellSize * cellCount);
            _cellSize = cellSize;
            Depth = depth;
            Parent = parent;
            _totalChildren = 0;

            // Calculate the size of the next recursion
            var childSize = cellSize / cellCount;

            // If there are no children, or if this cell is too small,
            // we store all children directly, and exit here.
            if (!items.Any() || childSize < minSize)
            {
                Items = items.Select(p => (p.ContainsEntireCell(Bounds), p)).ToArray();
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
                Items = items.Select(p => (p.ContainsEntireCell(Bounds), p)).ToArray();
                return;
            }

            // Generate the child cells using the already created bounds and child lists.
            Children = new QuadCell[cellCount, cellCount];
            for (var x = 0; x < cellCount; x++)
            for (var y = 0; y < cellCount; y++)
            {
                Children[x, y] = new QuadCell(this, (int)bounds[x, y].Xmin, (int)bounds[x, y].Ymin, childSize, cellCount, depth + 1, minSize, children[x, y]);
            }
        }

        public IEnumerable<IQuadTreeItem> Find(double x, double y)
        {
            if (Items != null)
                return Items.Where(p => p.FillsCell || p.Item.Contains(x, y)).Select(p => p.Item);
            return GetChild(x, y)?.Find(x, y) ?? Array.Empty<IQuadTreeItem>();
        }

        public (QuadCell Cell, IEnumerable<IQuadTreeItem> Items) FindCell(double x, double y, TreeNavigationStatistics stats = null)
        {
            var containsCheck = Contains(Bounds, x, y);
            stats?.IncrementBoundaryChecks(containsCheck.operations);
            if (!containsCheck.contains)
            {
                stats?.IncrementZoomOuts();
                if (Parent == null)
                {
                    stats?.IncrementOutside();
                    return (null, null);
                }
                return Parent.FindCell(x, y, stats);
            }

            if (Items != null)
            {
                stats?.IncrementLeafReturns(Items?.Count(p => !p.FillsCell) ?? 0);
                return (this, FindItems(x, y, stats));
            }

            stats?.IncrementZoomIns();
            return GetChild(x, y)?.FindCell(x, y, stats) ?? (null, null);
        }

        private (bool contains, int operations) Contains(BoundingBox2D bounds, double x, double y)
        {
            if (x < bounds.Xmin) return (false, 1);
            if (x > bounds.Xmax) return (false, 2);
            if (y < bounds.Ymin) return (false, 3);
            if (y > bounds.Ymax) return (false, 4);
            return (true, 4);
        }

        private IEnumerable<IQuadTreeItem> FindItems(double x, double y, TreeNavigationStatistics stats = null)
        {
            foreach (var item in Items)
            {
                stats?.IncreaseFillsCellCheck();
                if (item.FillsCell)
                {
                    stats?.IncreaseReturnedPolygon();
                    yield return item.Item;
                    continue;
                }

                stats?.IncreaseFullPolygonCheck(item.Item.GetEdgeCount());
                if (item.Item.Contains(x,y))
                {
                    stats?.IncreaseReturnedPolygon();
                    yield return item.Item;
                }
            }
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
                uncertainChildItems = Items?.Count(p => !p.FillsCell) ?? 0,
                populatedChildCells = Cells().Count()
            };

            return converter != null
                ? GeoJsonFeature.Polygon(Bounds.Corners, converter, props)
                : GeoJsonFeature.Polygon(Bounds.Corners, props);
        }
    }
}