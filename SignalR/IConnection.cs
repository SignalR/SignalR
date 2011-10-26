using System;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IConnection
    {
        TimeSpan ReceiveTimeout { get; set; }

        Task Send(object value);
        Task Broadcast(string message, object value);
        Task Broadcast(object value);

        Task<PersistentResponse> ReceiveAsync();
        Task<PersistentResponse> ReceiveAsync(long messageId);
    }
}
