using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using SignalR;
using SignalR.Hubs;
using SignalR.Infrastructure;
using SignalR.Transports;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]

namespace SignalR
{
    public static class PreApplicationStart
    {
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(HubModule));

            TransportManager.Register("longPolling", context => new LongPollingTransport(context, DependencyResolver.Resolve<IJsonStringifier>()));
            TransportManager.Register("forever", context => new ForeverTransport(context, DependencyResolver.Resolve<IJsonStringifier>()));
        }
    }
}