// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubContext : IHubContext
    {
        public HubContext(Func<string, ClientHubInvocation, IEnumerable<string>, Task> send, string hubName, IConnection connection)
        {
            Clients = new ExternalHubConnectionContext(send, hubName);
            Groups = new GroupManager(connection, hubName);
        }

        public IHubConnectionContext Clients { get; private set; }

        public IGroupManager Groups { get; private set; }

        private class ExternalHubConnectionContext : IHubConnectionContext
        {
            private readonly Func<string, ClientHubInvocation, IEnumerable<string>, Task> _send;
            private readonly string _hubName;

            public ExternalHubConnectionContext(Func<string, ClientHubInvocation, IEnumerable<string>, Task> send, string hubName)
            {
                _send = send;
                _hubName = hubName;
                All = AllExcept();
            }

            public dynamic All
            {
                get;
                private set;
            }

            public dynamic AllExcept(params string[] exclude)
            {
                return new ClientProxy(_send, _hubName, exclude);
            }

            public dynamic Group(string groupName, params string[] exclude)
            {
                return new SignalProxy(_send, groupName, _hubName, exclude);
            }

            public dynamic Client(string connectionId)
            {
                return new SignalProxy(_send, connectionId, _hubName);
            }
        }
    }
}
