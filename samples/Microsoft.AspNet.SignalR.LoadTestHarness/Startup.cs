using System;
using Microsoft.AspNet.SignalR.LoadTestHarness;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            app.MapSignalR<TestConnection>("/TestConnection");
            app.MapSignalR();

            Dashboard.Init();
        }
    }
}