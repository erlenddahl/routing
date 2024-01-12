using System;

namespace RoadNetworkRouting.Exceptions;

public class RoutingException : Exception
{
    public RoutingException(string message)
        : base(message)
    {
    }
}