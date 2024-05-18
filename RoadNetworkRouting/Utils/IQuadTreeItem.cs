using System.Collections.Generic;
using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Utils;

public interface IQuadTreeItem
{
    BoundingBox2D Bounds { get; }
    string Id { get; }

    bool Overlaps(BoundingBox2D bounds);
    bool Contains(double x, double y);
    bool ContainsEntireCell(BoundingBox2D bounds);
    int GetEdgeCount();
    IEnumerable<IQuadTreeItem> ChopToCell(BoundingBox2D bounds);
    IEnumerable<IQuadTreeItem> SplitInLeaf(BoundingBox2D bounds);
}