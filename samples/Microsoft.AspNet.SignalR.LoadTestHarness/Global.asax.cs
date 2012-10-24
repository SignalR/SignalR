using System;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(30);

            RouteTable.Routes.MapConnection<TestConnection>("TestConnection", "TestConnection/{*operation}");
            RouteTable.Routes.MapHubs();
            Dashboard.Init();
        }
    }
}
