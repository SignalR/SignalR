namespace SignalR.Transports
{
    public interface ITransportHeartBeat
    {
        void AddConnection(ITrackingConnection connection);
        void UpdateConnection(ITrackingConnection connection);
        void MarkConnection(ITrackingConnection connection);
    }
}
