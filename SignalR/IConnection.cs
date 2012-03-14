using System.Threading.Tasks;

namespace SignalR
{
    public interface IConnection : IReceivingConnection
    {
        Task Send(object value);
        Task Broadcast(string message, object value);
        Task Broadcast(object value);
    }
}