using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Microsoft.AspNet.SignalR.Hosting.AspNet.Routing;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(30);

            RouteTable.Routes.MapConnection<Shaft>("shaft", "shaft/{*operation}");
            RouteTable.Routes.MapHubs();
            StatsHub.Init();
        }
    }
}
