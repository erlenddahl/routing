using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class NegativeSearchRadiusIncrementException : RoutingException
{
    public NegativeSearchRadiusIncrementException(string message, QuickGraphSearchResult<RoadLink> result = null) : base(message, result)
    {
    }
}