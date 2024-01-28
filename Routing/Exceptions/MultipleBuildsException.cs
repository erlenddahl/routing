using System;

namespace Routing.Exceptions;

public class MultipleBuildsException : Exception
{
    public MultipleBuildsException(string message):base(message)
    {
    }
}