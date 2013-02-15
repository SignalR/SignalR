using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Owin;

namespace Microsoft.AspNet.Owin.Samples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapConnection<RawConnection>("/raw", new ConnectionConfiguration { EnableCrossDomain = true });
            app.MapHubs();
        }
    }
}
