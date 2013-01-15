using System;
using System.Configuration;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

[assembly: PreApplicationStartMethod(typeof(Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS.RegisterHubs), "Start")]

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public static class RegisterHubs
    {
        public static void Start()
        {
            string keepAliveRaw = ConfigurationManager.AppSettings["keepAlive"];
            string connectionTimeoutRaw = ConfigurationManager.AppSettings["connectionTimeout"];
            string disconnectTimeoutRaw = ConfigurationManager.AppSettings["disconnectTimeout"];

            int connectionTimeout;
            if (Int32.TryParse(connectionTimeoutRaw, out connectionTimeout))
            {
                GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout);
            }

            int disconnectTimeout;
            if (Int32.TryParse(disconnectTimeoutRaw, out disconnectTimeout))
            {
                GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout);
            }

            int keepAlive;
            if (String.IsNullOrEmpty(keepAliveRaw))
            {
                GlobalHost.Configuration.KeepAlive = null;
            }
            // Set only if the keep-alive was changed from the default value.
            else if (Int32.TryParse(keepAliveRaw, out keepAlive) && keepAlive != -1)
            {
                GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive);
            }

            var config = new HubConfiguration
            {
                EnableCrossDomain = true,
                EnableDetailedErrors = true
            };

            RouteTable.Routes.MapHubs("signalr.hubs2", "signalr2/test", new HubConfiguration());
            RouteTable.Routes.MapHubs(config);

            RouteTable.Routes.MapConnection<MyBadConnection>("errors-are-fun", "ErrorsAreFun");
            RouteTable.Routes.MapConnection<MyGroupEchoConnection>("group-echo", "group-echo");
            RouteTable.Routes.MapConnection<MySendingConnection>("multisend", "multisend");
            RouteTable.Routes.MapConnection<MyReconnect>("my-reconnect", "my-reconnect");
            RouteTable.Routes.MapConnection<MyGroupConnection>("groups", "groups");
            RouteTable.Routes.MapConnection<MyRejoinGroupsConnection>("rejoin-groups", "rejoin-groups");
            RouteTable.Routes.MapConnection<FilteredConnection>("filter", "filter");
            RouteTable.Routes.MapConnection<ConnectionThatUsesItems>("items", "items");
            RouteTable.Routes.MapConnection<SyncErrorConnection>("sync-error", "sync-error");
            RouteTable.Routes.MapConnection<AddGroupOnConnectedConnection>("add-group", "add-group");
            RouteTable.Routes.MapConnection<UnusableProtectedConnection>("protected", "protected");

            // End point to hit to verify the webserver is up
            RouteTable.Routes.Add("test-endpoint", new Route("ping", new TestEndPoint()));
        }
    }
}
