namespace Microsoft.AspNet.SignalR.Hubs
{
    public interface IHubActivator
    {
        IHub Create(HubDescriptor descriptor);
    }
}