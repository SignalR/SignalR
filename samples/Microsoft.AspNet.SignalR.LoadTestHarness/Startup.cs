using System;
using Owin;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            app.MapConnection<TestConnection>("TestConnection");
            app.MapHubs();

            Dashboard.Init();
        }
    }
}