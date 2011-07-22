namespace SignalR.Transports {
    internal interface ITrackingDisconnect {
        string ClientId { get;  }
        bool IsAlive { get; }
        void Disconnect();
    }
}