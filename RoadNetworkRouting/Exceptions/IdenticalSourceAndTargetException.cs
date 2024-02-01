using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class IdenticalSourceAndTargetException : RoutingException
{
    public IdenticalSourceAndTargetException(string message, QuickGraphSearchResult<RoadLink> result = null) : base(message, result)
    {
    }
}