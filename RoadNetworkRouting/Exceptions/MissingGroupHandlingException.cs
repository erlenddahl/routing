using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class MissingGroupHandlingException : RoutingException
{
    public MissingGroupHandlingException(string message, QuickGraphSearchResult<RoadLink> result = null) : base(message, result)
    {
    }
}