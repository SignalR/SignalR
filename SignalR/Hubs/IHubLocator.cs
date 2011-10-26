using System;
using System.Collections.Generic;

namespace SignalR.Hubs
{
    public interface IHubLocator
    {
        IEnumerable<Type> GetHubs();
    }
}