using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class MyReconnect : PersistentConnection
    {
        public int Reconnects { get; set; }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return null;
        }

        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            Reconnects++;
            return base.OnReconnected(request, connectionId);
        }
    }
}
