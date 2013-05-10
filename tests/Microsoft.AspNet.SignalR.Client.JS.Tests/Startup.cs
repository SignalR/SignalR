using Microsoft.AspNet.SignalR.Tests.Common;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Initializer.Configuration(app);
        }
    }
}