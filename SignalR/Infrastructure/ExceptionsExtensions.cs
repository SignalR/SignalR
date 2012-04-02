using System;

namespace SignalR
{
    public static class ExceptionsExtensions
    {
        public static Exception Unwrap(this Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            var unwrapped = ex.GetBaseException();
            var aggEx = unwrapped as AggregateException;
            if (aggEx != null)
            {
                // On mono GetBaseException() doesn't seem to do anything
                return aggEx.InnerException;
            }
            return unwrapped;
        }
    }
}

