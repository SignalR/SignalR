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
        /// Writes unbuffered data that is immediately available to the client connection. (e.g. chunked over http).
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A task that represents when the write operation is complete.</returns>
        Task WriteAsync(string data);

        /// <summary>
        /// Closes the connection to a client with optional data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A task that represents when the connection is closed.</returns>
        Task EndAsync(string data);
    }
}
