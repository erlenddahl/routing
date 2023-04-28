using RoadNetworkRouting.Exceptions;

namespace RoadNetworkRouting.Config;

public enum GroupHandling
{
    /// <summary>
    /// Allows only routing between the same interconnected network groups. If the entry points for the
    /// source and target locations are in different groups, a <see cref="DifferentGroupsException"/> will be thrown.
    /// </summary>
    OnlySame,

    /// <summary>
    /// Picks the most suitable network group (the one that minimized the total distance from entry points to
    /// source and target locations).
    /// </summary>
    BestGroup
}