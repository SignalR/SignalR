using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [Authorize]
    public class AuthenticatedEchoHub : EchoHub
    {
        public override Task OnConnected()
        {
            return Clients.All.SendUserOnConnected(Context.User.Identity.Name);            
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Clients.All.SendUserOnDisconnected(Context.User.Identity.Name);
            return base.OnDisconnected(stopCalled);
        }
    }
}
