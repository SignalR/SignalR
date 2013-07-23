﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Configuration
{
    /// <summary>
    /// Provides access to server configuration.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time a client should allow to connect before falling
        /// back to another transport or failing.
        /// </summary>
        TimeSpan TransportConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time to leave a connection open before timing out.
        /// </summary>
        TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time to wait after a connection goes away before raising the disconnect event.
        /// </summary>
        TimeSpan DisconnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time between send keep alive messages.
        /// If enabled, this value must be at least two seconds. Set to null to disable.
        /// </summary>
        TimeSpan? KeepAlive { get; set; }

        /// <summary>
        /// Gets of sets the number of messages to buffer for a specific signal.
        /// </summary>
        int DefaultMessageBufferSize { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing tell the client to wait before restablishing a
        /// long poll connection after data is sent from the server. Defaults to 0.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long", Justification = "Longpolling is a well known term")]
        TimeSpan LongPollDelay { get; set; }
    }
}
