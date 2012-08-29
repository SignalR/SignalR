using System;
using System.IO;
using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Represents a connection to the client.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Gets a value that determines if this client is still connected.
        /// </summary>
        bool IsClientConnected { get; }

        /// <summary>
        /// Gets or sets the content type of the response.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Writes buffered data.
        /// </summary>
        /// <param name="data">The data to write to the buffer.</param>
        void Write(ArraySegment<byte> data);

        /// <summary>
        /// Flushes the buffered response to the client.
        /// </summary>
        /// <returns>A task that represents when the data has been flushed.</returns>
        Task FlushAsync();

        /// <summary>
        /// Closes the connection to the client.
        /// </summary>
        /// <returns>A task that represents when the connection is closed.</returns>
        Task EndAsync();
    }
}
