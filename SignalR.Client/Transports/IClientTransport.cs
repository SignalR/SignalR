using System.Threading.Tasks;

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        void Start(Connection connection, string data);
        Task<T> Send<T>(Connection connection, string data);
        void Stop(Connection connection);
    }
}
