using System;
using AzureTestHarness;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace AzureTestHarness
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            //            app.MapSignalR<TestConnection>("/TestConnection");
            app.MapSignalR();

            //            Dashboard.Init();
        }
    }
}