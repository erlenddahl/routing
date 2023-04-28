using System;

namespace RoadNetworkRouting.Exceptions;

public class NoLinksFoundException : Exception
{
    public NoLinksFoundException(string msg) : base(msg)
    {
    }
}