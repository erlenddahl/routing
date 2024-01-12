using System;

namespace RoadNetworkRouting.Exceptions;

public class MissingConfigException : RoutingException
{
    public MissingConfigException(string msg) : base(msg)
    {
    }
}