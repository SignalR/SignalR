using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    // Awesome name, it rhymes
    public class RedirectionConnection : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            Connection.Send(connectionId, "Hi");

            return base.OnConnected(request, connectionId);
        }
    }
}
