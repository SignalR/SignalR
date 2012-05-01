using System;
using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    public interface IServerCommandHandler
    {
        Task SendCommand(ServerCommand command);

        Action<ServerCommand> Command { get; set; }
    }
}
