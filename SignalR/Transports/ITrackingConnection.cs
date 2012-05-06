using System;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    /// <summary>
    /// Represents a connection that can be tracked by an <see cref="ITransportHeartBeat"/>.
    /// </summary>
    public interface ITrackingConnection
    {
        /// <summary>
        /// Gets the id of the connection.
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Gets a value that represents if the connection is alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets a value that represents if the connection is timed out.
        /// </summary>
        bool IsTimedOut { get; }

        /// <summary>
        /// Gets a value indicating the amount of time to wait after the connection dies before firing the disconnecting the connection.
        /// </summary>
        TimeSpan DisconnectThreshold { get; }

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
        void KeepAlive();
    }
}