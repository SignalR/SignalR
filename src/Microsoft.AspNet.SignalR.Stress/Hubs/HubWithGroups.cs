// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Stress.Hubs
{
    public class HubWithGroups : Hub
    {
        public Task Join(string group)
        {
            return Groups.Add(Context.ConnectionId, group);
        }

        public Task Send(string group, int index)
        {
            return Clients.Group(group).Do(index);
        }

        public Task Leave(string group)
        {
            return Groups.Remove(Context.ConnectionId, group);
        }
    }
}
