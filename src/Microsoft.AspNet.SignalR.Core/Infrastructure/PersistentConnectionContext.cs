// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class PersistentConnectionContext : IPersistentConnectionContext
    {
        public PersistentConnectionContext(IConnection connection, IConnectionGroupManager groupManager)
        {
            Connection = connection;
            Groups = groupManager;
        }

        public IConnection Connection { get; private set; }

        public IConnectionGroupManager Groups { get; private set; }
    }
}
