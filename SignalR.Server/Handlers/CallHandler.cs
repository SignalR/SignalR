using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gate;
using Owin;

namespace SignalR.Server.Handlers
{
    public class CallHandler
    {
        private readonly IDependencyResolver _resolver;
        private readonly PersistentConnection _connection;

        private static readonly string[] AllowCredentialsTrue = new[] {"true"};

        public CallHandler(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

        public Task Invoke(IDictionary<string,object> env)
        {
            var req = new Request(env);
            var res = new Response(env);

            var serverRequest = new ServerRequest(req);
            var serverResponse = new ServerResponse(res, req.CancellationToken);
            var hostContext = new HostContext(serverRequest, serverResponse);

            var origin = req.Headers.GetHeaders("Origin");
            if (origin != null && origin.Any(sz => !String.IsNullOrEmpty(sz)))
            {
                serverResponse.Headers["Access-Control-Allow-Origin"] = origin;
                serverResponse.Headers["Access-Control-Allow-Credentials"] = AllowCredentialsTrue;
            }

            var disableRequestBuffering = env.Get<Action>("server.DisableRequestBuffering");
            if (disableRequestBuffering != null)
            {
                disableRequestBuffering();
            }

            var disableResponseBuffering = env.Get<Action>("server.DisableResponseBuffering");
            if (disableResponseBuffering != null)
            {
                disableResponseBuffering();
            }

            _connection.Initialize(_resolver);

            return _connection.ProcessRequestAsync(hostContext);
        }
    }
}