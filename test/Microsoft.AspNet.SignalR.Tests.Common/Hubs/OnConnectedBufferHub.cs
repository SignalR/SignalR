// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class OnConnectedBufferHub : Hub
    {
        public override async Task OnConnected()
        {
            await Clients.Caller.bufferMe(0);
            await Clients.Caller.bufferMe(1);

            await Task.Delay(TimeSpan.FromSeconds(5));

            await base.OnConnected();
        }

        public void Ping()
        {
            Clients.Caller.pong();
        }
    }
}
