using System;

namespace SignalR.Hubs
{
    public interface IHubActivator
    {
        IHub Create(Type hubType);
    }
}