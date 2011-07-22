using Microsoft.Web.Infrastructure.DynamicModuleHelper;

//[assembly: System.Web.PreApplicationStartMethod(typeof(PeerToPeerHttpSignalBusAppStart), "RegisterSignalReceiverModule")]

namespace SignalR.SignalBuses {
    public static class PeerToPeerHttpSignalBusAppStart {
        public static void RegisterSignalReceiverModule() {
            DynamicModuleUtility.RegisterModule(typeof(SignalReceiverModule));
        }
    }
}