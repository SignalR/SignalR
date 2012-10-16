using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting.AspNet;

[assembly: PreApplicationStartMethod(typeof($rootnamespace$.SignalRConfig), "Start")]

namespace $rootnamespace$
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