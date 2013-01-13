﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Represents a connection between client and server.
    /// </summary>
    public abstract class PersistentConnection
    {
        private const string WebSocketsTransportName = "webSockets";
        private const string ConnectionIdPurpose = "SignalR.ConnectionId";

        private IConfigurationManager _configurationManager;
        private ITransportManager _transportManager;
        private bool _initialized;
        private IServerCommandHandler _serverMessageHandler;

        public virtual void Initialize(IDependencyResolver resolver, HostContext context)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (_initialized)
            {
                return;
            }

            MessageBus = resolver.Resolve<IMessageBus>();
            JsonSerializer = resolver.Resolve<IJsonSerializer>();
            TraceManager = resolver.Resolve<ITraceManager>();
            Counters = resolver.Resolve<IPerformanceCounterManager>();
            AckHandler = resolver.Resolve<IAckHandler>();
            ProtectedData = resolver.Resolve<IProtectedData>();

            _configurationManager = resolver.Resolve<IConfigurationManager>();
            _transportManager = resolver.Resolve<ITransportManager>();
            _serverMessageHandler = resolver.Resolve<IServerCommandHandler>();

            _initialized = true;
        }

        public bool Authorize(IRequest request)
        {
            return AuthorizeRequest(request);
        }

        protected virtual TraceSource Trace
        {
            get
            {
                return TraceManager["SignalR.PersistentConnection"];
            }
        }

        protected IProtectedData ProtectedData { get; private set; }

        protected IMessageBus MessageBus { get; private set; }

        protected IJsonSerializer JsonSerializer { get; private set; }

        protected IAckHandler AckHandler { get; private set; }

        protected ITraceManager TraceManager { get; private set; }

        protected IPerformanceCounterManager Counters { get; private set; }

        protected ITransport Transport { get; private set; }

        /// <summary>
        /// Gets the <see cref="IConnection"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        public IConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="IConnectionGroupManager"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        public IConnectionGroupManager Groups
        {
            get;
            private set;
        }

        private string DefaultSignal
        {
            get
            {
                return GetType().FullName;
            }
        }

        /// <summary>
        /// Handles all requests for <see cref="PersistentConnection"/>s.
        /// </summary>
        /// <param name="context">The <see cref="HostContext"/> for the current request.</param>
        /// <returns>A <see cref="Task"/> that completes when the <see cref="PersistentConnection"/> pipeline is complete.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if connection wasn't initialized.
        /// Thrown if the transport wasn't specified.
        /// Thrown if the connection id wasn't specified.
        /// </exception>
        public virtual Task ProcessRequest(HostContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (!_initialized)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ConnectionNotInitialized));
            }

            if (IsNegotiationRequest(context.Request))
            {
                return ProcessNegotiationRequest(context);
            }
            else if (IsPingRequest(context.Request))
            {
                return ProcessPingRequest(context);
            }

            Transport = GetTransport(context);

            if (Transport == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ProtocolErrorUnknownTransport));
            }

            string connectionIdProtected = Transport.ConnectionId;

            // If there's no connection id then this is a bad request
            if (String.IsNullOrEmpty(connectionIdProtected))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ProtocolErrorMissingConnectionId));
            }

            string connectionId = null;

            try
            {
                connectionId = ProtectedData.Unprotect(connectionIdProtected, ConnectionIdPurpose);
            }
            catch
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ConnectionIdIncorrectFormat));
            }

            IEnumerable<string> signals = GetSignals(connectionId);
            IEnumerable<string> groups = OnRejoiningGroups(context.Request, Transport.Groups, connectionId);

            Connection connection = CreateConnection(connectionId, signals, groups);

            Connection = connection;
            Groups = new GroupManager(connection, DefaultSignal);

            Transport.TransportConnected = () =>
            {
                var command = new ServerCommand
                {
                    ServerCommandType = ServerCommandType.RemoveConnection,
                    Value = connectionId
                };

                return _serverMessageHandler.SendCommand(command);
            };

            Transport.Connected = () =>
            {
                return TaskAsyncHelper.FromMethod(() => OnConnected(context.Request, connectionId).OrEmpty());
            };

            Transport.Reconnected = () =>
            {
                return TaskAsyncHelper.FromMethod(() => OnReconnected(context.Request, connectionId).OrEmpty());
            };

            Transport.Received = data =>
            {
                Counters.ConnectionMessagesSentTotal.Increment();
                Counters.ConnectionMessagesSentPerSec.Increment();
                return TaskAsyncHelper.FromMethod(() => OnReceived(context.Request, connectionId, data).OrEmpty());
            };

            Transport.Disconnected = () =>
            {
                return TaskAsyncHelper.FromMethod(() => OnDisconnected(context.Request, connectionId).OrEmpty());
            };

            return Transport.ProcessRequest(connection).OrEmpty().Catch(Counters.ErrorsAllTotal, Counters.ErrorsAllPerSec);
        }

        private Connection CreateConnection(string connectionId, IEnumerable<string> signals, IEnumerable<string> groups)
        {
            return new Connection(MessageBus,
                                  JsonSerializer,
                                  DefaultSignal,
                                  connectionId,
                                  signals,
                                  groups,
                                  TraceManager,
                                  AckHandler,
                                  Counters);
        }

        /// <summary>
        /// Returns the default signals for the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <param name="connectionId">The id of the incoming connection.</param>
        /// <returns>The default signals for this <see cref="PersistentConnection"/>.</returns>
        private IEnumerable<string> GetDefaultSignals(string connectionId)
        {
            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)
            // 3. Ack signal

            return new string[] {
                DefaultSignal,
                connectionId,
                "ACK_" + connectionId
            };
        }

        /// <summary>
        /// Returns the signals used in the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <param name="connectionId">The id of the incoming connection.</param>
        /// <returns>The signals used for this <see cref="PersistentConnection"/>.</returns>
        protected virtual IEnumerable<string> GetSignals(string connectionId)
        {
            return GetDefaultSignals(connectionId);
        }

        /// <summary>
        /// Called before every request and gives the user a authorize the user.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <returns>A boolean value that represents if the request is authorized.</returns>
        protected virtual bool AuthorizeRequest(IRequest request)
        {
            return true;
        }

        /// <summary>
        /// Called when a connection reconnects after a timeout to determine which groups should be rejoined.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="groups">The groups the calling connection claims to be part of.</param>
        /// <param name="connectionId">The id of the reconnecting client.</param>
        /// <returns>A collection of group names that should be joined on reconnect</returns>
        protected virtual IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Called when a new connection is made.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the connecting client.</param>
        /// <returns>A <see cref="Task"/> that completes when the connect operation is complete.</returns>
        protected virtual Task OnConnected(IRequest request, string connectionId)
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when a connection reconnects after a timeout.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the re-connecting client.</param>
        /// <returns>A <see cref="Task"/> that completes when the re-connect operation is complete.</returns>
        protected virtual Task OnReconnected(IRequest request, string connectionId)
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when data is received from a connection.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the connection sending the data.</param>
        /// <param name="data">The payload sent to the connection.</param>
        /// <returns>A <see cref="Task"/> that completes when the receive operation is complete.</returns>
        protected virtual Task OnReceived(IRequest request, string connectionId, string data)
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when a connection disconnects.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the disconnected connection.</param>
        /// <returns>A <see cref="Task"/> that completes when the disconnect operation is complete.</returns>
        protected virtual Task OnDisconnected(IRequest request, string connectionId)
        {
            return TaskAsyncHelper.Empty;
        }

        private Task ProcessPingRequest(HostContext context)
        {
            var payload = new
            {
                Response = "pong"
            };

            if (!String.IsNullOrEmpty(context.Request.QueryString["callback"]))
            {
                return ProcessJsonpRequest(context, payload);
            }

            context.Response.ContentType = JsonUtility.MimeType;
            return context.Response.End(JsonSerializer.Stringify(payload));
        }

        private Task ProcessNegotiationRequest(HostContext context)
        {
            // Convert the keepAlive value to seconds based on the HeartBeat interval
            var keepAlive = _configurationManager.KeepAlive * _configurationManager.HeartbeatInterval.TotalSeconds;
            string connectionId = Guid.NewGuid().ToString("d");

            var payload = new
            {
                Url = context.Request.Url.LocalPath.Replace("/negotiate", ""),
                ConnectionId = ProtectedData.Protect(connectionId, ConnectionIdPurpose),
                KeepAlive = (keepAlive != 0) ? keepAlive : (double?)null,
                DisconnectTimeout = _configurationManager.DisconnectTimeout.TotalSeconds,
                TryWebSockets = _transportManager.SupportsTransport(WebSocketsTransportName) && context.SupportsWebSockets(),
                WebSocketServerUrl = context.WebSocketServerUrl(),
                ProtocolVersion = "1.1"
            };

            if (!String.IsNullOrEmpty(context.Request.QueryString["callback"]))
            {
                return ProcessJsonpRequest(context, payload);
            }

            context.Response.ContentType = JsonUtility.MimeType;
            return context.Response.End(JsonSerializer.Stringify(payload));
        }

        private Task ProcessJsonpRequest(HostContext context, object payload)
        {
            context.Response.ContentType = JsonUtility.JsonpMimeType;
            var data = JsonUtility.CreateJsonpCallback(context.Request.QueryString["callback"], JsonSerializer.Stringify(payload));

            return context.Response.End(data);
        }

        private static bool IsNegotiationRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPingRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/ping", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HostContext context)
        {
            return _transportManager.GetTransport(context);
        }
    }
}
