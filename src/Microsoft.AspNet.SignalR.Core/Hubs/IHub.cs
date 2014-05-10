﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public interface IHub : IDisposable
    {
        /// <summary>
        /// Gets a <see cref="HubCallerContext"/>. Which contains information about the calling client.
        /// </summary>
        HubCallerContext Context { get; set; }

        /// <summary>
        /// Gets a dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        IHubCallerConnectionContext<dynamic> Clients { get; set; }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> the hub instance.
        /// </summary>
        IGroupManager Groups { get; set; }

        /// <summary>
        /// Called when a new connection is made to the <see cref="IHub"/>.
        /// </summary>
        Task OnConnected();

        /// <summary>
        /// Called when a connection reconnects to the <see cref="IHub"/> after a timeout.
        /// </summary>
        Task OnReconnected();

        /// <summary>
        /// Called when a connection has disconnected gracefully from the <see cref="IHub"/>,
        /// i.e. stop was called on the client.
        /// </summary>
        Task OnDisconnected();

        /// <summary>
        /// Called when a connection is disconnected from the <see cref="IHub"/>.
        /// </summary>
        /// <param name="stopCalled">
        /// true, if stop was called on the client closing the connection gracefully;
        /// false, if the client timed out. Timeouts can be caused by clients reconnecting to another SignalR server in scaleout.
        /// </param>
        Task OnDisconnected(bool stopCalled);
    }
}

