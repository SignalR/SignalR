using System.Web.Routing;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Microsoft.AspNet.SignalR.Samples.Streaming;

namespace Microsoft.AspNet.SignalR.Samples
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapConnection<SendingConnection>("sending-connection", "sending-connection");
            routes.MapConnection<TestConnection>("test-connection", "test-connection");
            routes.MapConnection<RawConnection>("raw-connection", "raw-connection");
            routes.MapConnection<StreamingConnection>("streaming-connection", "streaming-connection");

            // Register the default hubs route /signalr
            routes.MapHubs();
        }
    }
}