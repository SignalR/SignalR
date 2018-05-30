// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.Owin.Samples.Hubs
{
    public class Chat : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public void Send(string message)
        {
            Clients.All.send(message);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }
    }
}
