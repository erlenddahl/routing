using System;
using System.Collections.Generic;
using System.Linq;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Utils;

public class QuadCell
{
    /// <summary>
    /// The size of this cell, in meters. Used for building the tree, and for looking up the correct child of a cell given a coordinate.
    /// </summary>
    private readonly int _cellSize;

    /// <summary>
    /// The depth of the three that this cell belongs to.
    /// </summary>
    public readonly int Depth;

    /// <summary>
    /// The total number of descendants of this cell. Used in statistics.
    /// </summary>
    private readonly int _totalChildren;

    /// <summary>
    /// The bounds of this cell.
    /// </summary>
    public BoundingBox2D Bounds { get; set; }

    /// <summary>
    /// The direct children of this cell. Is null if this cell has no children.
    /// </summary>
    public QuadCell[,] Children { get; set; }

    /// <summary>
    /// The parent of this cell. Used to quickly zoom out when reaching a cell edge.
    /// </summary>
    public QuadCell Parent { get; set; }

    /// <summary>
    /// The polygons that overlaps this cell. This is null for all non-leaf nodes.
    /// </summary>
    public (bool FillsCell, IQuadTreeItem Item)[] Items { get; set; }

    /// <summary>
    /// Helper property for checking if this cell overlaps any polygons.
    /// </summary>
    public bool IsEmpty => Items != null && !Items.Any();

    /// <summary>
    /// Helper property for checking if this cell is a leaf node (has no node children, may have polygon children).
    /// </summary>
    public bool IsLeaf => Items != null;

    public QuadCell(QuadCell parent, int xMin, int yMin, int cellSize, int cellCount, int depth, int minSize, IEnumerable<IQuadTreeItem> items)
    {
        Bounds = new BoundingBox2D(xMin, xMin + cellSize * cellCount, yMin, yMin + cellSize * cellCount);
        _cellSize = cellSize;
        Depth = depth;
        Parent = parent;
        _totalChildren = 0;

        // Calculate the size of the next recursion
        var childSize = cellSize / cellCount;

        // If there are no children, stop here.
        if (!items.Any())
        {
            Items = Array.Empty<(bool, IQuadTreeItem)>();
            return;
        }

        var checkedItems= items
            .SelectMany(p =>
            {
                // Checks if this polygon completely contains this cell. That means 
                // that we are always inside this polygon when we are inside this cell,
                // removing the need for a full polygon check.
                var entirelyContained = p.ContainsEntireCell(Bounds);

                if (entirelyContained) return new[]{ (entirelyContained: true, relevantPart: p) };

                // If it is not entirely contained, we need to do a polygon check.
                // Because a polygon check is expensive, usually around 7 operations 
                // per edge in the polygon, we want to reduce the number of edges.
                // We can do this by cutting away the parts of the polygon that is
                // outside of the cell, and just keep the tiny part that is inside.
                var parts = p.ChopToCell(Bounds);
                if (parts == null) return Array.Empty<(bool, IQuadTreeItem)>();
                return parts.Select(p => (entirelyContained: false, relevantPart: p));
            })
            // If ChopToCell returns null, which it may do if the polygon only touches the cell,
            // we want to ignore it.
            .Where(p => p.relevantPart != null)
            .ToArray();

        // If the size of any child cells would be too small, this cell
        // is a leaf node containing all the given items. Check if the
        // items are completely or partially covered, and store them.
        if (childSize < minSize)
        {
            Items = checkedItems.SelectMany(p =>
            {
                if (p.entirelyContained) return new[] { p };
                return p.relevantPart.SplitInLeaf(Bounds).Select(c => (false, c));
            }).ToArray();
            return;
        }

        // And finally, if all the given items completely contain this cell, we don't need to recurse any more,
        // since all descendants of this cell will contain all the same items.
        if (checkedItems.All(p => p.entirelyContained))
        {
            Items = checkedItems;
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
        foreach (var item in checkedItems)
        {
            _totalChildren++;
            var added = false;

            for (var x = 0; x < cellCount; x++)
            for (var y = 0; y < cellCount; y++)
            {
                // If the bounds for this cell overlap with this
                // child, put it in the corresponding cell.
                if (item.relevantPart.Overlaps(bounds[x, y]))
                {
                    children[x, y].Add(item.relevantPart);
                    added = true;
                }
            }

            if (!added)
            {
                throw new Exception("Child with bounds [" + item.relevantPart.Bounds + "] did not match any of the children of the cell with bounds [" + Bounds + "].");
            }
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
            if (Parent == null)
            {
                stats?.IncrementOutside();
                return (null, null);
            }
            stats?.IncrementZoomOuts();
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
        var returned = new HashSet<string>();
        var wasInside = new List<(bool FillsCell, IQuadTreeItem Item)>();
        var wasNotInside = new List<(bool FillsCell, IQuadTreeItem Item)>();
        foreach (var item in Items)
        {
            if (returned.Contains(item.Item.Id))
            {
                wasInside.Add(item);
                continue;
            }

            stats?.IncreaseFillsCellCheck();
            if (item.FillsCell)
            {
                stats?.IncreaseReturnedPolygon();
                returned.Add(item.Item.Id);
                yield return item.Item;
                wasInside.Add(item);
                continue;
            }

            stats?.IncreaseFullPolygonCheck(item.Item.GetEdgeCount());
            if (item.Item.Contains(x,y))
            {
                stats?.IncreaseReturnedPolygon();
                returned.Add(item.Item.Id);
                yield return item.Item;
                wasInside.Add(item);
                continue;
            }

            wasNotInside.Add(item);
        }

        Items = wasInside.Concat(wasNotInside).ToArray();
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