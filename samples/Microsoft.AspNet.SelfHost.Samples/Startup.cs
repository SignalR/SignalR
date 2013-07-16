using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tracing;
using Owin;

namespace Microsoft.AspNet.SelfHost.Samples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/raw-connection", subApp =>
            {
                subApp.UseConnection<RawConnection>();
            });

            app.MapHubs();

            // Turn tracing on programmatically
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;
        }
    }
}
