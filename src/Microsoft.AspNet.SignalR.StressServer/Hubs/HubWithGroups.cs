// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.StressServer.Hubs
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

    public class OnConnectedOnDisconnectedHub : Hub
    {
        private static List<string> theList = new List<string>();
        private static readonly object syncLock = new object();

        public string Echo(string str)
        {
            return str;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            lock (syncLock)
            {
                theList.Remove("TheList_" + Context.ConnectionId);
            }

            Groups.Remove(Context.ConnectionId, "Groups_" + Context.ConnectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnConnected()
        {
            lock (syncLock)
            {
                theList.Add("TheList_" + Context.ConnectionId);
            }

            Groups.Add(Context.ConnectionId, "Groups_" + Context.ConnectionId);

            return base.OnConnected();
        }
    }
}
