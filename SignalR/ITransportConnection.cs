using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    public interface ITransportConnection
    {
        IDisposable Receive(string messageId, Func<Exception, PersistentResponse, Task> callback);

        Task<PersistentResponse> ReceiveAsync(CancellationToken timeoutToken);
        Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken timeoutToken);

        Task Send(string signal, object value);
    }
}