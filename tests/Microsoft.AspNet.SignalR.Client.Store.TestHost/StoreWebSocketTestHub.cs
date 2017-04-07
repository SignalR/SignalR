// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System;

namespace Microsoft.AspNet.SignalR.Client.UWP.TestHost
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
