using System;

namespace Domain.Exceptions
{
    public class GoneException : Exception
    {
        public GoneException(string message) : base(message)
        {
        }
    }
}
