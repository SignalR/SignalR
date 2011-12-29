using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using SignalR.AspNet;
using SignalR.Transports;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]

namespace SignalR.AspNet
{
    public static class PreApplicationStart
    {
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(HubModule));

            TransportManager.InitializeDefaultTransports();
        }
    }
}