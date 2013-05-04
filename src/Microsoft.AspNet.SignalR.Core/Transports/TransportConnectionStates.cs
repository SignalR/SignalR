using System;

namespace Microsoft.AspNet.SignalR.Transports
{
    [Flags]
    public enum TransportConnectionStates
    {
        Initialized = 0,
        Added = 1,
        Removed = 2,
        Replaced = 4,
        QueueDrained = 8,
        HttpRequestEnded = 16
    }
}
