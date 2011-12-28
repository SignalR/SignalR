using System;
using System.IO;
using System.Web;
using SignalR.Aspet;

namespace SignalR.Hubs
{
    public class HubModule : IHttpModule
    {
        private const string Url = "~/signalr";

        public void Init(HttpApplication app)
        {
            app.PostResolveRequestCache += (sender, e) =>
            {
                if (app.Request.AppRelativeCurrentExecutionFilePath.StartsWith(Url, StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetExtension(app.Request.AppRelativeCurrentExecutionFilePath).Equals(".js", StringComparison.OrdinalIgnoreCase))
                {
                    var dispatcher = new HubDispatcher(Url);
                    var handler = new PersistentConnectionHandler(dispatcher);

                    app.Context.RemapHandler(handler);
                }
            };
        }

        public void Dispose() { }
    }
}