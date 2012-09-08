using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Server.Util;

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
            var serverRequest = new ServerRequest(env);
            var serverResponse = new ServerResponse(env);
            var hostContext = new HostContext(serverRequest, serverResponse);

            var origin = serverRequest.RequestHeaders.GetHeaders("Origin");
            if (origin != null && origin.Any(sz => !String.IsNullOrEmpty(sz)))
            {
                serverResponse.ResponseHeaders["Access-Control-Allow-Origin"] = origin;
                serverResponse.ResponseHeaders["Access-Control-Allow-Credentials"] = AllowCredentialsTrue;
            }

            serverRequest.DisableRequestBuffering();
            serverResponse.DisableResponseBuffering();

            _connection.Initialize(_resolver);

            return _connection.ProcessRequestAsync(hostContext);
        }
    }
}