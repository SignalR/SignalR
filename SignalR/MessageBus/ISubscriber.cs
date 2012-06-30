using System;
using System.Collections.Generic;

namespace SignalR
{
    public interface ISubscriber
    {
        IEnumerable<string> EventKeys { get; }

        event Action<string, string> EventAdded;

        event Action<string> EventRemoved;
    }
}
