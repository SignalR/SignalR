using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;

[assembly: PreApplicationStartMethod(typeof(Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS.RegisterHubs), "Start")]

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public static class RegisterHubs
    {
        public static void Start()
        {
            // Register the default hubs route: ~/signalr/hubs
            RouteTable.Routes.MapHubs();
        }
    }
}
