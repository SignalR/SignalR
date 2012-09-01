using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    public interface ITransportConnection
    {
        IDisposable Receive(string messageId, Func<PersistentResponse, Task<bool>> callback, int maxMessages);

        Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken cancel, int maxMessages);

        Task Send(string signal, object value);
    }
}