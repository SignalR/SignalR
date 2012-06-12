using System;
using System.Web;

namespace SignalR.Hosting.AspNet.WebSockets
{
    internal sealed class WebSocketFixModule : IHttpModule
    {

        public void Dispose()
        {
            // no-op
        }

        public void Init(HttpApplication context)
        {
            context.EndRequest += (sender, e) =>
            {
                HttpContext httpContext = ((HttpApplication)sender).Context;
                HttpResponse httpResponse = httpContext.Response;
                if (httpContext.IsWebSocketRequestUpgrading && httpResponse.StatusCode == 101)
                {
                    // If the client disconnects before this call to Flush(), an exception will be thrown
                    // and normal ASP.NET exception handling will kick in. If the client disconnects after
                    // the call to Flush(), the handshake will have already been completed by the time
                    // WebSocketPipeline.ProcessRequest() executes, which prevents the exception from being
                    // thrown from within that method and bringing down the process.
                    httpResponse.Flush();
                }
            };
        }

    }
}
