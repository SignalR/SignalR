// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [HubName("groupChat")]
    public class ChatWithGroups : Hub
    {
        public Task Send(string group, string message)
        {
            return Clients.Group(group).send(message);
        }

        public Task Join(string group)
        {
            return Groups.Add(Context.ConnectionId, group);
        }

        public Task Leave(string group)
        {
            return Groups.Remove(Context.ConnectionId, group);
        }
    }
}
