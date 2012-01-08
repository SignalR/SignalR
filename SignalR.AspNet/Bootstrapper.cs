using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using SignalR.AspNet;
using SignalR.Hubs;
using SignalR.Infrastructure;

[assembly: PreApplicationStartMethod(typeof(Bootstrapper), "Initialize")]

namespace SignalR.AspNet
{
    public static class Bootstrapper
    {
        private static bool _initialized;
        private static object _lockObject = new object();

        public static void Initialize()
        {
            // Ensure this is only called once
            if (!_initialized)
            {
                lock (_lockObject)
                {
                    if (!_initialized)
                    {
                        RegisterHubModule();

                        // Replace defaults with asp.net implementations
                        var typeResolver = new BuildManagerTypeResolver();
                        var hubLocator = new BuildManagerTypeLocator();

                        DependencyResolver.Register(typeof(IHubTypeResolver), () => typeResolver);
                        DependencyResolver.Register(typeof(IHubLocator), () => hubLocator);

                        _initialized = true;
                    }
                }
            }
        }

        private static void RegisterHubModule()
        {
            try
            {
                DynamicModuleUtility.RegisterModule(typeof(HubModule));
            }
            catch
            {
                // If we're unable to load MWI then just swallow the exception and don't allow
                // the automagic hub registration
            }
        }
    }
}