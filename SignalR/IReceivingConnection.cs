using System;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IReceivingConnection
    {
        TimeSpan ReceiveTimeout { get; set; }

        Task<PersistentResponse> ReceiveAsync();
        Task<PersistentResponse> ReceiveAsync(string messageId);

        Task SendCommand(SignalCommand command);
    }
}