using SignalR.Hubs.Lookup.Descriptors;

namespace SignalR.Hubs
{
    public interface IHubActivator
    {
        IHub Create(HubDescriptor descriptor);
    }
}