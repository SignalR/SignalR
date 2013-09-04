// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubContext : IHubContext
    {
        public HubContext(IDuplexConnection connection, IHubPipelineInvoker invoker, string hubName, JsonSerializer serializer)
        {
            Connection = connection;
            Clients = new HubConnectionContextBase(connection, invoker, hubName);
            Groups = new GroupManager(connection, PrefixHelper.GetHubGroupName(hubName));
            Serializer = serializer;
        }

        public IHubConnectionContext<dynamic> Clients { get; private set; }

        public JsonSerializer Serializer { get; private set; }

        public IGroupManager Groups { get; private set; }

        public IDuplexConnection Connection { get; private set; }
    }
}
