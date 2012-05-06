using System;
using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Handles commands from server to server.
    /// </summary>
    public interface IServerCommandHandler
    {
        /// <summary>
        /// Sends a command to all connected servers.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task SendCommand(ServerCommand command);

        /// <summary>
        /// Gets or sets a callback that is invoked when a command is received.
        /// </summary>
        Action<ServerCommand> Command { get; set; }
    }
}
