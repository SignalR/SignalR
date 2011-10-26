using System;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public interface ITransport
    {
        event Action<string> Received;
        event Action Connected;
        event Action Disconnected;
        event Action<Exception> Error;
        Func<Task> ProcessRequest(IConnection connection);
        void Send(object value);
    }
}
