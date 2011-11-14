namespace SignalR.Transports
{
    public interface ITransportHeartBeat
    {
        void AddConnection(ITrackingDisconnect connection);
        void MarkConnection(ITrackingDisconnect connection);
    }
}
