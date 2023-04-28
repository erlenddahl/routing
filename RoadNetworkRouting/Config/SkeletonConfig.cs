using System.Collections.Generic;
using System.IO;

namespace RoadNetworkRouting.Config;

public class SkeletonConfig
{
    public string LinkDataDirectory { get; set; }
    public int LinksPerFile { get; set; } = 100;

    public Dictionary<int, int> LinkIdToFileNumber { get; set; } = new();

    public void SetSequence(int linkId)
    {
        LinkIdToFileNumber.Add(linkId, LinkIdToFileNumber.Count / LinksPerFile);
    }

    public string GetLinkDataFile(int id)
    {
        return Path.Combine(LinkDataDirectory, LinkIdToFileNumber[id] + ".bin");
    }
}