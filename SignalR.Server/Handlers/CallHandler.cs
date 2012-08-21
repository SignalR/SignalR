using System;
using System.Linq;
using System.Threading.Tasks;
using Gate;
using Owin;
using SignalR.Server.Utils;

namespace SignalR.Server.Handlers
{
    public class CallHandler
    {
        readonly IDependencyResolver _resolver;
        readonly PersistentConnection _connection;

        static readonly string[] AllowCredentialsTrue = new[] { "true" };

        public CallHandler(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var req = new Request(call);

            var tcs = new TaskCompletionSource<ResultParameters>();

            var serverRequest = new ServerRequest(req);
            var serverResponse = new ServerResponse(req.Completed, tcs);
            var hostContext = new HostContext(serverRequest, serverResponse);

            var origin = req.Headers.GetHeaders("Origin");
            if (origin != null && origin.Any(sz => !String.IsNullOrEmpty(sz)))
            {
                serverResponse.Headers["Access-Control-Allow-Origin"] = origin;
                serverResponse.Headers["Access-Control-Allow-Credentials"] = AllowCredentialsTrue;
            }

            var disableRequestBuffering = call.Get<Action>("server.DisableRequestBuffering");
            if (disableRequestBuffering != null)
            {
                disableRequestBuffering();
            }

            var disableResponseBuffering = call.Get<Action>("server.DisableResponseBuffering");
            if (disableResponseBuffering != null)
            {
                disableResponseBuffering();
            }

            _connection.Initialize(_resolver);

            _connection
                .ProcessRequestAsync(hostContext)
                .Finally(serverResponse.End, runSynchronously: true);

            return tcs.Task;
        }
    }
}
