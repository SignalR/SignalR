using System;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public interface ITrackingDisconnect
    {
        string ConnectionId { get; }
        bool IsAlive { get; }
        TimeSpan DisconnectThreshold { get; }
        Task Disconnect();
    }
}