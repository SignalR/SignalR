using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public interface ITransport
    {
        event Action<string> Received;
        event Action Connected;
        event Action Disconnected;
        event Action<Exception> Error;
        Func<Task> ProcessRequest(IReceivingConnection connection);
        IEnumerable<string> Groups { get; }
        Task Send(object value);
        string ConnectionId { get; }
    }
}
