using System;

namespace SignalR.Hubs
{
    using SignalR.Hubs.Lookup.Descriptors;

    public interface IHubActivator
    {
        IHub Create(HubDescriptor descriptor);
    }
}