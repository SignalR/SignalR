using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting.AspNet;

[assembly: PreApplicationStartMethod(typeof(Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.SignalRConfig), "Start")]

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples
{
    public static class SignalRConfig
    {
        public static void Start()
        {
            // Create the magic /signalr/hubs route
            RouteTable.Routes.MapHubs();            
        }
    }
}