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
        }
    }
}
