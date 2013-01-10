// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;

namespace Microsoft.AspNet.SignalR.Owin
{
    public class CallHandler
    {
        private readonly IDependencyResolver _resolver;
        private readonly PersistentConnection _connection;

        private static readonly string[] AllowCredentialsTrue = new[] { "true" };

        private static bool _supportWebSockets;
        private static bool _supportWebSocketsInitialized;
        private static object _supportWebSocketsLock = new object();

        public CallHandler(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

        public Task Invoke(IDictionary<string, object> environment)
        {
            var serverRequest = new ServerRequest(environment);
            var serverResponse = new ServerResponse(environment);
            var hostContext = new HostContext(serverRequest, serverResponse);

            // Add CORS support
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
                () => environment.SupportsWebSockets());

            hostContext.Items[HostConstants.ShutdownToken] = environment.GetShutdownToken();
            hostContext.Items[HostConstants.DebugMode] = environment.GetIsDebugEnabled();

            serverRequest.DisableRequestBuffering();
            serverResponse.DisableResponseBuffering();

            _connection.Initialize(_resolver, hostContext);

            return _connection.ProcessRequest(hostContext);
        }
    }
}
