using System.Threading.Tasks;

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        Task<NegotiationResponse> Negotiate(Connection connection, string url);
        Task Start(Connection connection, string data);
        Task<T> Send<T>(Connection connection, string data);
        void Stop(Connection connection);
    }
}
