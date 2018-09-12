using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Microsoft.AspNet.SignalR.ServiceSample.Startup))]

namespace Microsoft.AspNet.SignalR.ServiceSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapAzureSignalR(typeof(Startup).FullName);
        }
    }
}
