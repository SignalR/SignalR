using System.IO;

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
        /// The response stream
        /// </summary>
        Stream OutputStream { get; }
    }
}
