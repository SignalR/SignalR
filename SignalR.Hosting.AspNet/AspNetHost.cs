using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SignalR.Hosting;
using SignalR.Infrastructure;

namespace SignalR.Hosting.AspNet
{
    public class AspNetHost : HttpTaskAsyncHandler
    {
        private static readonly IDependencyResolver _defaultResolver = new DefaultDependencyResolver();
        private static IDependencyResolver _resolver;

        private readonly PersistentConnection _connection;
        
        private static readonly Lazy<bool> _hasAcceptWebSocketRequest =
            new Lazy<bool>(() =>
            {
                return typeof(HttpContextBase).GetMethods().Any(m => m.Name.Equals("AcceptWebSocketRequest", StringComparison.OrdinalIgnoreCase));
            });

        public AspNetHost(PersistentConnection connection)
        {
            _connection = connection;
        }

        public static IDependencyResolver DependencyResolver
        {
            get { return _resolver ?? _defaultResolver; }
        }

        public override Task ProcessRequestAsync(HttpContextBase context)
        {
            var request = new AspNetRequest(context.Request);
            var response = new AspNetResponse(context);
            var hostContext = new HostContext(request, response, context.User);

            // Determine if the client should bother to try a websocket request
            hostContext.Items[HostConstants.SupportsWebSockets] = _hasAcceptWebSocketRequest.Value;

            // Set the debugging flag
            hostContext.Items[HostConstants.DebugMode] = context.IsDebuggingEnabled;

            // Stick the context in here so transports or other asp.net specific logic can
            // grab at it.
            hostContext.Items["System.Web.HttpContext"] = context;

            // Initialize the connection
            _connection.Initialize(DependencyResolver);

            return _connection.ProcessRequestAsync(hostContext);
        }

        public static void SetResolver(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            _resolver = resolver;
        }

    }
}
