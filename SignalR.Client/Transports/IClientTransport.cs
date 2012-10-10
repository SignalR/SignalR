using System.Threading.Tasks;
using System.Threading;
using System;

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        Task<NegotiationResponse> Negotiate(IConnection connection);
        Task Start(IConnection connection, string data);
        Task<T> Send<T>(IConnection connection, string data);
        void Stop(IConnection connection);
        void Stop(IConnection connection, bool notifyServer);

        void RegisterKeepAlive(TimeSpan keepAlive);                
        void MonitorKeepAlive(IConnection connection);
        void StopMonitoringKeepAlive();

        bool SupportsKeepAlive { get; set; }
    }
}
