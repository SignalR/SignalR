using Microsoft.AspNet.SignalR.Tests.Common;
using Owin;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Initializer.Configuration(app);
        }
    }
}
