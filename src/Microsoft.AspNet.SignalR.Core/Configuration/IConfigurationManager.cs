// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
        /// The default value is 5 seconds.
        /// </summary>
        TimeSpan TransportConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time to leave a connection open before timing out.
        /// The default value is 110 seconds.
        /// </summary>
        TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time to wait after a connection goes away before raising the disconnect event.
        /// The default value is 30 seconds.
        /// </summary>
        TimeSpan DisconnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing the amount of time between send keep alive messages.
        /// If enabled, this value must be at least two seconds. Set to null to disable.
        /// The default value is 10 seconds.
        /// </summary>
        TimeSpan? KeepAlive { get; set; }

        /// <summary>
        /// Gets or sets the number of messages to buffer for a specific signal.
        /// The default value is 1000.
        /// </summary>
        int DefaultMessageBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size in bytes of messages sent from client to the server via WebSockets.
        /// Set to null to disable this limit.
        /// The default value is 65536 or 64 KB.
        /// </summary>
        int? MaxIncomingWebSocketMessageSize { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> representing tell the client to wait before restablishing a
        /// long poll connection after data is sent from the server. 
        /// The default value is 0.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long", Justification = "Longpolling is a well known term")]
        TimeSpan LongPollDelay { get; set; }
    }
}
