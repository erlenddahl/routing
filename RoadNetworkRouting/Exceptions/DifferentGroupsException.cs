namespace RoadNetworkRouting.Exceptions;

public class DifferentGroupsException : RoutingException
{
    public DifferentGroupsException(string msg) : base(msg)
    {
    }
}