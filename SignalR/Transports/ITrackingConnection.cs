using System;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public interface ITrackingConnection
    {
        string ConnectionId { get; }
        bool IsAlive { get; }
        bool IsTimedOut { get; }
        TimeSpan DisconnectThreshold { get; }
        Task Disconnect();
        void Timeout();
        void KeepAlive();
    }
}