using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Owin;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Owin
{
    public class BasicAuthApplication
    {
        public void Configuration(IAppBuilder app)
        {
            // TODO: Figure out how to not have this on all the time
            app.UseType<BasicAuthModule>("user", "password");
            app.MapHubs("/signalr");
        }
    }
}
