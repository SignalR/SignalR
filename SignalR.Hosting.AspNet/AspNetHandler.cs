using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Hosting.AspNet
{
    public class AspNetHandler : HttpTaskAsyncHandler
    {
        // This will fire when the app domain is shutting down
        internal static readonly CancellationTokenSource AppDomainTokenSource = new CancellationTokenSource();

        private readonly PersistentConnection _connection;
        private readonly IDependencyResolver _resolver;

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
            var request = new AspNetRequest(context);
            var response = new AspNetResponse(context);
            var hostContext = new HostContext(request, response);

#if NET45
            // Determine if the client should bother to try a websocket request
            hostContext.Items[HostConstants.SupportsWebSockets] = true;
#endif

            // Set the debugging flag
            hostContext.Items[HostConstants.DebugMode] = context.IsDebuggingEnabled;

            // Set the host shutdown token
            hostContext.Items[HostConstants.ShutdownToken] = AppDomainTokenSource.Token;

            // Stick the context in here so transports or other asp.net specific logic can
            // grab at it.
            hostContext.Items["System.Web.HttpContext"] = context;

            // Initialize the connection
            _connection.Initialize(_resolver);

            return _connection.ProcessRequestAsync(hostContext);
        }
    }
}
