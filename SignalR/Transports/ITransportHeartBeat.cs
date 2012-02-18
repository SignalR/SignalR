namespace SignalR.Transports
{
    public interface ITransportHeartBeat
    {
        void AddConnection(ITrackingConnection connection);
        void MarkConnection(ITrackingConnection connection);
    }
}
