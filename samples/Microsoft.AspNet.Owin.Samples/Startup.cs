using Owin;

namespace Microsoft.AspNet.Owin.Samples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Map hubs
            app.MapHubs("/signalr");
        }
    }
}
