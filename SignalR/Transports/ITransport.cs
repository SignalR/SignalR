using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public interface ITransport
    {
        Func<string, Task> Received { get; set; }
        Func<Task> Connected { get; set; }
        Func<Task> TransportConnected { get; set; }
        Func<Task> Reconnected { get; set; }
        Func<Task> Disconnected { get; set; }
        Func<Exception, Task> Error { get; set; }
        string ConnectionId { get; }
        IEnumerable<string> Groups { get; }

        Task ProcessRequest(ITransportConnection connection);
        Task Send(object value);
    }
}
