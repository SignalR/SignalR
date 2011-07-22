using System;

namespace SignalR.Hubs {
    public interface IHubActivator {
        Hub Create(Type hubType);
    }
}