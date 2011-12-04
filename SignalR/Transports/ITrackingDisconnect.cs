namespace SignalR.Transports
{
    public interface ITrackingDisconnect
    {
        string ConnectionId { get; }
        bool IsAlive { get; }
        void Disconnect();
    }
}