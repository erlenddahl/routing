using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class IdenticalSearchPointsException : RoutingException
{
    public IdenticalSearchPointsException(string message, QuickGraphSearchResult<RoadLink> result = null) : base(message, result)
    {
    }
}