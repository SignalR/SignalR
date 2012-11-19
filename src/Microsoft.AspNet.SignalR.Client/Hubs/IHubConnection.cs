using System;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public interface IHubConnection : IConnection
    {
        string RegisterCallback(Action<HubResult> callback);
    }
}
