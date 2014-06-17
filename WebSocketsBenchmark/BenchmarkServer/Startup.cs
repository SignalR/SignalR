using BenchmarkServer;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(Startup))]
namespace BenchmarkServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            app.MapSignalR();

            Dashboard.Init();
        }
    }
}
