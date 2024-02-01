using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class EmptyRouteException : RoutingException
{
    public EmptyRouteException(string message, QuickGraphSearchResult<RoadLink> result = null) : base(message, result)
    {
    }
}