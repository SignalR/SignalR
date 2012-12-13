namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public enum TransportType
    {
        Auto,
        Websockets,
        ServerSentEvents,
        ForeverFrame,
        LongPolling,
    }
}
