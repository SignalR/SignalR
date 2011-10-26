namespace SignalR.Transports
{
    public interface ITransportHeartBeat
    {
        void AddConnection(ITrackingDisconnect connection);
        void RemoveConnection(ITrackingDisconnect connection);
    }
}
