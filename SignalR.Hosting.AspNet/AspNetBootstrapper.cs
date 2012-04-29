using System.Web;
using System.Web.Routing;
using SignalR.Hosting.AspNet;

[assembly: PreApplicationStartMethod(typeof(AspNetBootstrapper), "Initialize")]

namespace SignalR.Hosting.AspNet
{
    /// <summary>
    /// Initializes the AspNet hosting pipeline
    /// </summary>
    public static class AspNetBootstrapper
    {
        private static bool _initialized;
        private static object _lockObject = new object();
        private static readonly AspNetShutDownDetector _detector = new AspNetShutDownDetector(OnAppDomainShutdown);

        /// <summary>
        /// Initializes the ASP.NET host and sets up the default hub route (~/signalr). Do not call this from your code.
        /// </summary>
        public static void Initialize()
        {
            if (!_initialized)
            {
                lock (_lockObject)
                {
                    if (!_initialized)
                    {
                        RouteTable.Routes.MapHubs();
                     
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