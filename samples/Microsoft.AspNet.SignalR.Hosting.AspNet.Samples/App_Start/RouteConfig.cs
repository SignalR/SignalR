using System.Web.Routing;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Microsoft.AspNet.SignalR.Samples.Streaming;

namespace Microsoft.AspNet.SignalR.Samples
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapConnection<SendingConnection>("sending-connection", "sending-connection/{*operation}");
            routes.MapConnection<TestConnection>("test-connection", "test-connection/{*operation}");
            routes.MapConnection<RawConnection>("raw-connection", "raw-connection/{*operation}");
            routes.MapConnection<StreamingConnection>("streaming-connection", "streaming-connection/{*operation}");

            // Register the default hubs route: ~/signalr/hubs
            routes.MapHubs();
        }
    }
}