using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    internal static class ConnectionExtensions
    {
        public static Task Close(this ITransportConnection connection, string connectionId)
        {
            var command = new SignalCommand
            {
                Type = CommandType.Disconnect
            };

            return connection.Send(SignalCommand.AddCommandSuffix(connectionId), command);
        }

        public static Task Abort(this ITransportConnection connection, string connectionId)
        {
            var command = new SignalCommand
            {
                Type = CommandType.Abort
            };

            return connection.Send(SignalCommand.AddCommandSuffix(connectionId), command);
        }
    }
}
