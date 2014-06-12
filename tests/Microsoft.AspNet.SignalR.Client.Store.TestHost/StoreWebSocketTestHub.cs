using System.Collections.Generic;
using System;

namespace Microsoft.AspNet.SignalR.Client.Store.TestHost
{
    public class StoreWebSocketTestHub : Hub
    {
        public void Echo(string message)
        {
            Clients.All.echo(message);
        }

        public IEnumerable<int> ForceReconnect()
        {
            yield return 1;
            // throwing here will close the websocket which should trigger reconnect
            throw new Exception();
        }
    }
}
