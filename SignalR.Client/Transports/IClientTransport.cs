using System.Threading.Tasks;

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        Task Start(Connection connection, string data);
        Task<T> Send<T>(Connection connection, string data);
        void Stop(Connection connection);
    }
}
