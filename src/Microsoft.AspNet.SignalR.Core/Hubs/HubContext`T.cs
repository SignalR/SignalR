// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubContext<T> : IHubContext<T>
    {
        public HubContext(IHubContext dynamicContext)
        {
            // Validate will throw an InvalidOperationException if T is an invalid type
            TypedClientBuilder<T>.Validate();

            Clients = new TypedHubConnectionContext<T>(dynamicContext.Clients);
            Groups = dynamicContext.Groups;
        }

        public IHubConnectionContext<T> Clients { get; private set; }

        public IGroupManager Groups { get; private set; }
    }
}
