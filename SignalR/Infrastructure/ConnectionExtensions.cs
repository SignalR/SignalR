using System;
using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    public static class ConnectionExtensions
    {
        public static Task Close(this ITransportConnection connection)
        {
            var command = new SignalCommand
            {
                Type = CommandType.Disconnect
            };

            return connection.SendCommand(command);
        }

        public static Task Abort(this ITransportConnection connection)
        {
            var command = new SignalCommand
            {
                Type = CommandType.Abort
            };

            return connection.SendCommand(command);
        }

        public static Task SendCommand(this IConnection connection, string connectionId, SignalCommand command)
        {
            return connection.Send(SignalCommand.AddCommandSuffix(connectionId), command);
        }
    }
}
