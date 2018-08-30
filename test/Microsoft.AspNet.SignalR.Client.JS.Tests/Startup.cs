using System;
using System.IO;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // We want faster tests!
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(110);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(30);
            Initializer.ConfigureRoutes(app, GlobalHost.DependencyResolver);

            app.UseFileServer(new FileServerOptions()
            {
                EnableDefaultFiles = true,
                FileSystem = new PhysicalFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });
        }
    }
}
