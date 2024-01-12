using System;

namespace RoadNetworkRouting.Exceptions;

public class NoLinksFoundException : RoutingException
{
    public NoLinksFoundException(string msg) : base(msg)
    {
    }
}