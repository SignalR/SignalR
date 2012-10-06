using System;

namespace SignalR.Hubs
{
    class NotAuthorizedException : Exception
    {
        public NotAuthorizedException() { }
        public NotAuthorizedException(string message) : base(message) { }
    }
}
