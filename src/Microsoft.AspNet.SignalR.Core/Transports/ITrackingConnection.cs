// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Transports
{
    /// <summary>
    /// Represents a connection that can be tracked by an <see cref="ITransportHeartbeat"/>.
    /// </summary>
    public interface ITrackingConnection : IDisposable
    {
        /// <summary>
        /// Gets the id of the connection.
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Gets a cancellation token that represents the connection's lifetime.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the task that completes when the task returned by PersistentConnection.OnConnected does.
        /// </summary>
        Task ConnectTask { get; }

        /// <summary>
        /// Gets a value that represents if the connection is alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets a value that represents if the connection is timed out.
        /// </summary>
        bool IsTimedOut { get; }

        /// <summary>
        /// Gets a value that represents if the connection supprots keep alive.
        /// </summary>
        bool SupportsKeepAlive { get; }

        /// <summary>
        /// Gets a value that represents if the connection should timeout after inactivity.
        /// </summary>
        bool RequiresTimeout { get; }

        /// <summary>
        /// Gets a value indicating the amount of time to wait after the connection dies before firing the disconnecting the connection.
        /// </summary>
        TimeSpan DisconnectThreshold { get; }

        /// <summary>
        /// Gets the uri of the connection.
        /// </summary>
        Uri Url { get; }

        /// <summary>
        /// Applies a new state to the connection.
        /// </summary>
        void ApplyState(TransportConnectionStates states);

        /// <summary>
        /// Causes the connection to disconnect.
        /// </summary>
        Task Disconnect();

        /// <summary>
        /// Causes the connection to timeout.
        /// </summary>
        void Timeout();

        /// <summary>
        /// Sends a keep alive ping over the connection.
        /// </summary>
        Task KeepAlive();

        /// <summary>
        /// Increments performance counter for current connections.
        /// </summary>
        void IncrementConnectionsCount();

        /// <summary>
        /// Decrements performance counter for current connections.
        /// </summary>
        void DecrementConnectionsCount();

        /// <summary>
        /// Kills the connection.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End", Justification = "Ends the connction thus the name is appropriate.")]
        void End();
    }
}
