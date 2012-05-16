#if NET20
using SignalR.Client.Net20.Infrastructure;
#else
using System.Threading.Tasks;
#endif

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        Task<NegotiationResponse> Negotiate(IConnection connection);
        Task Start(IConnection connection, string data);
        Task<T> Send<T>(IConnection connection, string data);
        void Stop(IConnection connection);
    }
}
