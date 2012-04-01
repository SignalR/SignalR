using System.Web;
using System.Web.Routing;
using SignalR.Hosting.AspNet;
using SignalR.Hosting.AspNet.Routing;

[assembly: PreApplicationStartMethod(typeof(AspNetBootstrapper), "Initialize")]

namespace SignalR.Hosting.AspNet
{
    public static class AspNetBootstrapper
    {
        private static bool _initialized;
        private static object _lockObject = new object();
        private static readonly AspNetShutDownDetector _detector = new AspNetShutDownDetector(OnAppDomainShutdown);

        public static void Initialize()
        {
            if (!_initialized)
            {
                lock (_lockObject)
                {
                    if (!_initialized)
                    {
                        RouteTable.Routes.MapHubs("~/signalr");
                     
                        _detector.Initialize();

                        _initialized = true;
                    }
                }
            }
        }

        private static void OnAppDomainShutdown()
        {
            // Close all connections before the app domain goes down.
            // Only signal all connections on a particular appdomain
            AspNetHandler.AppDomainTokenSource.Cancel();
        }
    }
}