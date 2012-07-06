using System;
using System.Text;
using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Extension methods for <see cref="IResponse"/>.
    /// </summary>
    public static class ResponseExtensions
    {
        /// <summary>
        /// Closes the connection to a client with optional data.
        /// </summary>
        /// <param name="response">The <see cref="IResponse"/>.</param>
        /// <param name="data">The data to write to the connection.</param>
        /// <returns>A task that represents when the connection is closed.</returns>
        public static Task EndAsync(this IResponse response, string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return response.EndAsync(new ArraySegment<byte>(bytes));
        }
    }
}
