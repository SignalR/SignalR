using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;

[assembly: PreApplicationStartMethod(typeof(Microsoft.AspNet.SignalR.Client.JS.Tests.RegisterHubs), "Start")]

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public static class RegisterHubs
    {
        public static void Start()
        {
            RouteTable.Routes.MapHubs();
        }
    }
}
