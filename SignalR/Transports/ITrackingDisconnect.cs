using System;
namespace SignalR.Transports
{
    public interface ITrackingDisconnect
    {
        string ConnectionId { get; }
        bool IsAlive { get; }
        TimeSpan DisconnectThreshold { get; }
        void Disconnect();
    }
}