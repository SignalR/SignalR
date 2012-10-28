using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.Samples
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapConnection<SendingConnection>("sending-connection", "sending-connection/{*operation}");
            routes.MapConnection<TestConnection>("test-connection", "test-connection/{*operation}");
            routes.MapConnection<Raw>("raw", "raw/{*operation}");
            routes.MapConnection<Streaming>("streaming", "streaming/{*operation}");

            // Register the default hubs route: ~/signalr/hubs
            routes.MapHubs();
        }
    }
}