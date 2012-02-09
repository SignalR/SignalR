using System;
using System.Threading.Tasks;

namespace SignalR
{
    public static class ConnectionExtensions
    {
        public static Task Close(this IReceivingConnection connection)
        {
            var command = new SignalCommand
            {
                Type = CommandType.Disconnect,
                ExpiresAfter = TimeSpan.FromMinutes(30)
            };

            return connection.SendCommand(command);
        }
    }
}
