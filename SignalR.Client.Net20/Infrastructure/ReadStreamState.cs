using System.IO;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// Internal class to hold state details inside the async state when readaing from stream.
    /// </summary>
    internal class ReadStreamState
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
        public Task<int> Response { get; set; }
    }
}