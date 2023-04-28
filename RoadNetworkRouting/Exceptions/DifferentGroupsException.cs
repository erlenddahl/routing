using System;

namespace RoadNetworkRouting.Exceptions;

public class DifferentGroupsException : Exception
{
    public DifferentGroupsException(string msg) : base(msg)
    {
    }
}