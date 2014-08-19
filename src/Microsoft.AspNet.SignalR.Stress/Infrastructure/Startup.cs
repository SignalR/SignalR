using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Properties["host.AppName"] = "Stress";
            app.MapSignalR();
            app.MapSignalR<StressConnection>("/echo");
            GlobalHost.DependencyResolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
        }
    }
}
