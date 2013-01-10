using System;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            RouteTable.Routes.MapConnection<TestConnection>("TestConnection", "TestConnection");

            var config = new HubConfiguration
            {
                EnableJavaScriptProxies = true
            };

            RouteTable.Routes.MapHubs(config);
            Dashboard.Init();
        }
    }
}
