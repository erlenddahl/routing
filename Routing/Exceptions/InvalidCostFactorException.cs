using System;

namespace Routing.Exceptions;

public class InvalidCostFactorException : Exception
{
    public InvalidCostFactorException(string message) : base(message)
    {
    }
}