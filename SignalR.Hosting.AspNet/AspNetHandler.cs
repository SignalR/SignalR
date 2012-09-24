﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SignalR.Hosting.AspNet
{
    public class AspNetHandler : HttpTaskAsyncHandler
    {
        // This will fire when the app domain is shutting down
        internal static readonly CancellationTokenSource AppDomainTokenSource = new CancellationTokenSource();

        private readonly PersistentConnection _connection;
        private readonly IDependencyResolver _resolver;

        private const string WebSocketVersionServerVariable = "WEBSOCKET_VERSION";

        public AspNetHandler(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

#if NET45
        public override Task ProcessRequestAsync(HttpContext context)
        {
            return ProcessRequestAsync(new HttpContextWrapper(context));
        }

        public Task ProcessRequestAsync(HttpContextBase context)
#else
        public override Task ProcessRequestAsync(HttpContextBase context)
#endif
        {
            // https://developer.mozilla.org/En/HTTP_Access_Control
            string origin = context.Request.Headers["Origin"];
            if (!String.IsNullOrEmpty(origin))
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
            }

            var request = new AspNetRequest(context);
            var response = new AspNetResponse(context);
            var hostContext = new HostContext(request, response);

            // Determine if the client should bother to try a websocket request
#if NET45
            hostContext.Items[HostConstants.SupportsWebSockets] = HttpRuntime.IISVersion != null && HttpRuntime.IISVersion.Major >= 8 && !String.IsNullOrEmpty(context.Request.ServerVariables[WebSocketVersionServerVariable]);
#else
            hostContext.Items[HostConstants.SupportsWebSockets] = false;
#endif
            // Set the debugging flag
            hostContext.Items[HostConstants.DebugMode] = context.IsDebuggingEnabled;

            // Set the host shutdown token
            hostContext.Items[HostConstants.ShutdownToken] = AppDomainTokenSource.Token;

            // Stick the context in here so transports or other asp.net specific logic can
            // grab at it.
            hostContext.Items["System.Web.HttpContext"] = context;

            // Initialize the connection
            _connection.Initialize(_resolver, hostContext);

            try
            {
                return _connection.ProcessRequestAsync(hostContext);
            }
            catch (NotSupportedException) // WebSockets not supported
            {
                context.Response.StatusCode = 501; // HTTP 501 Not Implemented
                return TaskAsyncHelper.Empty;
            }
        }
    }
}
