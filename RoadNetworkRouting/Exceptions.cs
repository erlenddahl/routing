using System;

namespace RoadNetworkRouting;

public class MissingConfigException : Exception
{
    public MissingConfigException(string msg) : base(msg)
    {
    }
}

public class NoLinksFoundException : Exception
{
    public NoLinksFoundException(string msg) : base(msg)
    {
    }
}

public class DifferentGroupsException : Exception
{
    public DifferentGroupsException(string msg) : base(msg)
    {
    }
}