// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class TestHub : Hub
    {
        public Task Echo(string data)
        {
            return Clients.Caller.invoke(data);
        }

        public Task Broadcast(string data)
        {
            return Clients.All.invoke(data);
        }
    }
}