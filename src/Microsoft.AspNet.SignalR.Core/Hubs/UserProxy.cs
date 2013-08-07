using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class UserProxy : SignalProxy
    {
        public UserProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubUserPrefix, ListHelper<string>.Empty)
        {

        }
    }
}
