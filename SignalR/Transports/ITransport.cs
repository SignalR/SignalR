using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    /// <summary>
    /// Represents a transport that communicates
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Gets or sets a callback that is invoked when the transport receives data.
        /// </summary>
        Func<string, Task> Received { get; set; }

        /// <summary>
        /// Gets or sets a callback that is invoked when the initial connection connects to the transport.
        /// </summary>
        Func<Task> Connected { get; set; }

        /// <summary>
        /// Gets or sets a callback that is invoked when the transport connects.
        /// </summary>
        Func<Task> TransportConnected { get; set; }

        /// <summary>
        /// Gets or sets a callback that is invoked when the transport reconnects.
        /// </summary>
        Func<Task> Reconnected { get; set; }

        /// <summary>
        /// Gets or sets a callback that is invoked when the transport disconnects.
        /// </summary>
        Func<Task> Disconnected { get; set; }

        /// <summary>
        /// Gets or sets a callback that is invoked when the transport throws an error.
        /// </summary>
        Func<Exception, Task> Error { get; set; }

        /// <summary>
        /// Gets the connection id for the transport.
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Gets the groups the connection is a member of.
        /// </summary>
        IEnumerable<string> Groups { get; }

        /// <summary>
        /// Processes the specified <see cref="ITransportConnection"/> for this transport.
        /// </summary>
        /// <param name="connection">The <see cref="ITransportConnection"/> to process.</param>
        /// <returns>A <see cref="Task"/> that completes when the transport has finished processing the connection.</returns>
        Task ProcessRequest(ITransportConnection connection);

        /// <summary>
        /// Sends data over the transport.
        /// </summary>
        /// <param name="value">The value to be sent.</param>
        /// <returns>A <see cref="Task"/> that completes when the send is complete.</returns>
        Task Send(object value);
    }
}
