// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Provides methods that communicate with SignalR connections that connected to a <see cref="Hub"/>.
    /// </summary>
    public abstract class Hub<T> : Hub where T : class
    {
        // This allows the typed Client property to be settable for testing
        private IHubCallerConnectionContext<T> _testClients;

        /// <summary>
        /// Gets a dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        public new IHubCallerConnectionContext<T> Clients
        {
            get
            {
                return _testClients ?? new TypedHubCallerConnectionContext<T>(base.Clients);
            }
            set
            {
                _testClients = value;
            }
        }
    }
}
