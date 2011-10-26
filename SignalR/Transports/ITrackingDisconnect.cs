namespace SignalR.Transports
{
    public interface ITrackingDisconnect
    {
        string ClientId { get; }
        bool IsAlive { get; }
        void Disconnect();
    }
}