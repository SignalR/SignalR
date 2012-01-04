namespace SignalR.Client.Transports
{
    public static class Transport
    {
        public static readonly IClientTransport LongPolling = new LongPollingTransport();
        public static readonly IClientTransport ServerSentEvents = new ServerSentEventsTransport();
    }
}
