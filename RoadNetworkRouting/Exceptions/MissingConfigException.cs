using System;

namespace RoadNetworkRouting.Exceptions;

public class MissingConfigException : Exception
{
    public MissingConfigException(string msg) : base(msg)
    {
    }
}