using System.Threading.Tasks;
using System.Web;
using SignalR.Abstractions;
using SignalR.Web;

namespace SignalR.AspNet
{
    public class PersistentConnectionHandler : HttpTaskAsyncHandler
    {
        private readonly PersistentConnection _connection;

        public PersistentConnectionHandler(PersistentConnection connection)
        {
            _connection = connection;
        }

        public override Task ProcessRequestAsync(HttpContextBase context)
        {
            var request = new AspNetRequest(context.Request);
            var response = new AspNetResponse(context.Request, context.Response);
            var hostContext = new HostContext(request, response, context.User);

            // Set the debugging flag
            hostContext.Items["debugMode"] = context.IsDebuggingEnabled;

            // Stick the context in here so transports or other asp.net specific logic can
            // grab at it.
            hostContext.Items["aspnet.context"] = context;

            return _connection.ProcessRequestAsync(hostContext);
        }
    }
}
