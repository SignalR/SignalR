using System.Web;

namespace SignalR.SignalBuses {
    internal class SignalReceiverModule : IHttpModule {

        public void Init(HttpApplication context) {
            context.PostResolveRequestCache += (sender, e) => {
                // Remap to SignalReceiverHandler
                if (context.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/" + SignalReceiverHandler.HandlerName)) {
                    var handler = new SignalReceiverHandler();
                    context.Context.RemapHandler(handler);
                }
            };
        }

        public void Dispose() { }

    }
}