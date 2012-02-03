using System.Threading.Tasks;

namespace SignalR
{
    public interface IReceivingConnection
    {
        Task<PersistentResponse> ReceiveAsync();
        Task<PersistentResponse> ReceiveAsync(string messageId);

        Task SendCommand(SignalCommand command);
    }
}