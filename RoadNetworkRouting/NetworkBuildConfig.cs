using System;

namespace RoadNetworkRouting;

public class NetworkBuildConfig
{
    public double MaxDistanceLinkSplit { get; set; } = 1;
    public double MaxDistanceNodeConnection { get; set; } = 1;
    public bool PerformLinkSplit { get; set; } = true;
    public Action<double> ProgressReporter { get; set; }
    public Action<string> StateReporter { get; set; }
}