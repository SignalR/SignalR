using System.Web;

namespace SignalR.Web
{
    internal class MessageReceiverModule : IHttpModule
    {

        public void Init(HttpApplication context)
        {
            context.PostResolveRequestCache += (sender, e) =>
            {
                // Remap to MessageReceiverHandler
                if (context.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/" + MessageReceiverHandler.HandlerName))
                {
                    var handler = new MessageReceiverHandler();
                    context.Context.RemapHandler(handler);
                }
            };
        }

        public void Dispose() { }

    }
}