namespace Microsoft.AspNet.SignalR.Hubs
{
    public interface IHubDeactivator
    {
        void Destruct(HubActivationResult hubActivationResult);
    }
}