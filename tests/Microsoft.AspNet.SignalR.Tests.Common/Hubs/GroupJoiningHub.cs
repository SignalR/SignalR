// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class GroupJoiningHub : Hub
    {
        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, Context.ConnectionId).Wait();

            return PingGroup();
        }

        public Task PingGroup()
        {
            return Clients.Group(Context.ConnectionId).ping();
        }
    }
}
