namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public enum TransportType
    {
        Websockets,
        ServerSentEvents,
        ForeverFrame,
        LongPolling,
    }
}
