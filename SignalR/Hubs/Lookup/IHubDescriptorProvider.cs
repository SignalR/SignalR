namespace SignalR.Hubs.Lookup
{
    using System.Collections.Generic;

    using SignalR.Hubs.Lookup.Descriptors;

    public interface IHubDescriptorProvider
    {
        IEnumerable<HubDescriptor> GetHubs();

        bool TryGetHub(string hubName, out HubDescriptor descriptor);
    }
}