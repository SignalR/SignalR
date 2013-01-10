using Microsoft.AspNet.SignalR;
using Owin;

namespace Microsoft.AspNet.Owin.Samples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapHubs();
        }
    }
}
