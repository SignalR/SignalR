using System;
using System.IO;
using System.Web;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet
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

                    // Setup asp.net specific dependencies for the hub's dependency resolver
                    AspNetBootstrapper.InitializeHubDependencies();

                    // Get the absolute url
                    string url = VirtualPathUtility.ToAbsolute(Url);

                    // Create the hub dispatcher
                    var connection = new HubDispatcher(url);

                    var host = new AspNetHost(connection);

                    app.Context.RemapHandler(host);
                }
            };
        }

        public void Dispose() { }
    }
}