using System;
using RoadNetworkRouting.Network;
using Routing;

namespace RoadNetworkRouting.Exceptions;

public class RoutingException : Exception
{
    public QuickGraphSearchResult<RoadLink> Result { get; set; }

    public RoutingException(string message, QuickGraphSearchResult<RoadLink> result = null)
        : base(message)
    {
        Result = result;
    }
}