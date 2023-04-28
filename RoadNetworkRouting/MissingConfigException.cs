using System;

namespace RoadNetworkRouting;

public class MissingConfigException : Exception
{
    public MissingConfigException(string msg) : base(msg)
    {
    }
}