// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class GroupJoiningHub : Hub
    {
        public override async Task OnConnected()
        {
            await Groups.Add(Context.ConnectionId, Context.ConnectionId);
            // Add this delay to fix the race?
            //await Task.Delay(1000);
            await PingGroup();
        }

        public async Task PingGroup()
        {
            await Clients.Group(Context.ConnectionId).ping();
        }
    }
}
