using System;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IReceivingConnection
    {
        Task<PersistentResponse> ReceiveAsync();
        Task<PersistentResponse> ReceiveAsync(ulong messageId);

        Task SendCommand(SignalCommand command);
    }
}