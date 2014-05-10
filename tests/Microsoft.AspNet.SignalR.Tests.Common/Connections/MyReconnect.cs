using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class MyReconnect : PersistentConnection
    {
        private readonly Action _onReconnected;

        public MyReconnect()
            : this(onReconnected: () => { })
        {
        }

        public MyReconnect(Action onReconnected)
        {
            _onReconnected = onReconnected;
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return null;
        }

        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            _onReconnected();
            return base.OnReconnected(request, connectionId);
        }
    }
}
