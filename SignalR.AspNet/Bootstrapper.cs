using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using SignalR.AspNet;
using SignalR.Hubs;
using SignalR.Infrastructure;
using SignalR.Transports;

[assembly: PreApplicationStartMethod(typeof(Bootstrapper), "Start")]

namespace SignalR.AspNet
{
    public static class Bootstrapper
    {
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(HubModule));

            // Replace defaults with asp.net implementations
            var typeResolver = new BuildManagerTypeResolver();
            var hubLocator = new BuildManagerTypeLocator();

            DependencyResolver.Register(typeof(IHubTypeResolver), () => typeResolver);
            DependencyResolver.Register(typeof(IHubLocator), () => hubLocator);


            TransportManager.InitializeDefaultTransports();
        }
    }
}