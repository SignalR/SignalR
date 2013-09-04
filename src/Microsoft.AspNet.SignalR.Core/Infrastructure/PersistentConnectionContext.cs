// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class PersistentConnectionContext : IPersistentConnectionContext
    {
        public PersistentConnectionContext(IDuplexConnection connection)
        {
            Connection = connection;
            Groups = new GroupManager(connection, PrefixHelper.GetPersistentConnectionGroupName(connection.DefaultSignal));
        }

        public IDuplexConnection Connection { get; private set; }

        public IConnectionGroupManager Groups { get; private set; }
    }
}
