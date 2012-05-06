using System.IO;

namespace SignalR.Client.Http
{
    /// <summary>
    /// The http response.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Reads the response body as a string.
        /// </summary>
        /// <returns></returns>
        string ReadAsString();

        /// <summary>
        /// Gets the steam that represents the response body.
        /// </summary>
        /// <returns></returns>
        Stream GetResponseStream();

        /// <summary>
        /// Closes the response.
        /// </summary>
        void Close();
    }
}
