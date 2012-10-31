// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Server.Infrastructure;

namespace Microsoft.AspNet.SignalR.Server.Handlers
{
    public class CallHandler
    {
        private readonly IDependencyResolver _resolver;
        private readonly PersistentConnection _connection;

        private static readonly string[] AllowCredentialsTrue = new[] { "true" };

        private static bool _supportWebSockets;
        private static bool _supportWebSocketsInitialized;
        private static Object _supportWebSocketsLock = new Object();

        public CallHandler(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var serverRequest = new ServerRequest(env);
            var serverResponse = new ServerResponse(env);
            var hostContext = new HostContext(serverRequest, serverResponse);

            var origins = serverRequest.RequestHeaders.GetHeaders("Origin");
            if (origins != null && origins.Any(origin => !String.IsNullOrEmpty(origin)))
            {
                serverResponse.ResponseHeaders["Access-Control-Allow-Origin"] = origins;
                serverResponse.ResponseHeaders["Access-Control-Allow-Credentials"] = AllowCredentialsTrue;
            }

            hostContext.Items[HostConstants.SupportsWebSockets] = LazyInitializer.EnsureInitialized(
                ref _supportWebSockets, 
                ref _supportWebSocketsInitialized,
                ref _supportWebSocketsLock,
                () => SupportsWebSockets(env));

            serverRequest.DisableRequestBuffering();
            serverResponse.DisableResponseBuffering();

            _connection.Initialize(_resolver, hostContext);

            return _connection.ProcessRequestAsync(hostContext);
        }

        private bool SupportsWebSockets(IDictionary<string, object> env)
        {
            object value;
            if (env.TryGetValue(OwinConstants.ServerCapabilities, out value) && value is IDictionary<string,object>)
            {
                var capabilities = (IDictionary<string, object>) value;
                return capabilities.ContainsKey(OwinConstants.WebSocketVersion);
            }
            return false;
        }
    }
}
