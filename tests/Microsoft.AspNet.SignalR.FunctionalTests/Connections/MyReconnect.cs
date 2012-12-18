using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyReconnect : PersistentConnection
    {
        public int Reconnects { get; set; }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return null;
        }

        protected override Task OnReconnectedAsync(IRequest request, string connectionId)
        {
            Reconnects++;
            return base.OnReconnectedAsync(request, connectionId);
        }
    }
}
