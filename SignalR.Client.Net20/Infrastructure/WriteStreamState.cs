using System.IO;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// Internal class to hold state details inside the async state when writing to stream.
    /// </summary>
    internal class WriteStreamState
    {
        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the callback task.
        /// </summary>
        public Task Response { get; set; }
    }
}