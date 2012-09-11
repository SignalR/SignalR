using System;
using System.Collections.Generic;

namespace SignalR
{
    public interface ISubscriber
    {
        IEnumerable<string> EventKeys { get; }

        string Identity { get; }

        event Action<string> EventAdded;

        event Action<string> EventRemoved;
    }
}
