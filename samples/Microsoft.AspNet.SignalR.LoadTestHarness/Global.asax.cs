using System;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.StressServer.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);
            RouteTable.Routes.MapConnection<TestConnection>("TestConnection", "TestConnection");
            RouteTable.Routes.MapHubs();

            Dashboard.Init();
        }
    }
}
