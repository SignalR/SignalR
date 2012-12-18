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
            string heartbeatIntervalRaw = ConfigurationManager.AppSettings["heartbeatInterval"];
            string enableRejoiningGroupsRaw = ConfigurationManager.AppSettings["enableRejoiningGroups"];

            int keepAlive;
            if (Int32.TryParse(keepAliveRaw, out keepAlive))
            {
                GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive);
            }
            else
            {
                GlobalHost.Configuration.KeepAlive = null;
            }

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

            int heartbeatInterval;
            if (Int32.TryParse(heartbeatIntervalRaw, out heartbeatInterval))
            {
                GlobalHost.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(heartbeatInterval);
            }

            bool enableRejoiningGroups;
            if (Boolean.TryParse(enableRejoiningGroupsRaw, out enableRejoiningGroups) &&
                enableRejoiningGroups)
            {
                GlobalHost.HubPipeline.EnableAutoRejoiningGroups();
            }

            // Register the default hubs route: ~/signalr/hubs
            RouteTable.Routes.MapHubs();
            RouteTable.Routes.MapConnection<MyBadConnection>("errors-are-fun", "ErrorsAreFun/{*operation}");
            RouteTable.Routes.MapConnection<MyGroupEchoConnection>("group-echo", "group-echo/{*operation}");
            RouteTable.Routes.MapConnection<MySendingConnection>("multisend", "multisend/{*operation}");
            RouteTable.Routes.MapConnection<MyReconnect>("my-reconnect", "my-reconnect/{*operation}");
            RouteTable.Routes.MapConnection<MyGroupConnection>("groups", "groups/{*operation}");
            RouteTable.Routes.MapConnection<MyRejoinGroupsConnection>("rejoin-groups", "rejoin-groups/{*operation}");
            RouteTable.Routes.MapConnection<FilteredConnection>("filter", "filter/{*operation}");
            RouteTable.Routes.MapConnection<ConnectionThatUsesItems>("items", "items/{*operation}");
            RouteTable.Routes.MapConnection<SyncErrorConnection>("sync-error", "sync-error/{*operation}");

            // End point to hit to verify the webserver is up
            RouteTable.Routes.Add("test-endpoint", new Route("ping", new TestEndPoint()));
        }
    }
}
