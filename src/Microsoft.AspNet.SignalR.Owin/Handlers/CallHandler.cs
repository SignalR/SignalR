// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public Task Invoke(IDictionary<string, object> env)
        {
            var serverRequest = new ServerRequest(env);
            var serverResponse = new ServerResponse(env);
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
                () => SupportsWebSockets(env));

            hostContext.Items[HostConstants.ShutdownToken] = GetShutdownToken(env);
            hostContext.Items[HostConstants.DebugMode] = GetIsDebugEnabled(env);

            serverRequest.DisableRequestBuffering();
            serverResponse.DisableResponseBuffering();

            _connection.Initialize(_resolver, hostContext);

            return _connection.ProcessRequestAsync(hostContext);
        }

        internal static CancellationToken GetShutdownToken(IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostOnAppDisposing, out value)
                && value is CancellationToken
                ? (CancellationToken)value
                : default(CancellationToken);
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

        private object GetIsDebugEnabled(IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostAppModeKey, out value)
                && value is string && !String.IsNullOrWhiteSpace(value as string)
                && OwinConstants.AppModeDevelopment.Equals(value as string, StringComparison.OrdinalIgnoreCase);
        }
    }
}
