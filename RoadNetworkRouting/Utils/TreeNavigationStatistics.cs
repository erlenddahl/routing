using System.Text.Json;

namespace RoadNetworkRouting.Utils;

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
    public long EdgesChecked { get; set; }

    public long Operations => BoundaryLowLevelChecks + ZoomedIn * 4 + ZoomedOut + EdgesChecked * 8;

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

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
    }
}