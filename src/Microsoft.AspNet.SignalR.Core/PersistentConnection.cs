// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Represents a connection between client and server.
    /// </summary>
    public abstract class PersistentConnection
    {
        private const string WebSocketsTransportName = "webSockets";
        private const string PingJsonPayload = "{ \"Response\": \"pong\" }";
        private const string StartJsonPayload = "{ \"Response\": \"started\" }";
        private static readonly char[] SplitChars = new[] { ':' };
        private static readonly ProtocolResolver _protocolResolver = new ProtocolResolver();

        private IConfigurationManager _configurationManager;
        private ITransportManager _transportManager;
        private bool _initialized;

        public virtual void Initialize(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (_initialized)
            {
                return;
            }

            MessageBus = resolver.Resolve<IMessageBus>();
            JsonSerializer = resolver.Resolve<JsonSerializer>();
            TraceManager = resolver.Resolve<ITraceManager>();
            Counters = resolver.Resolve<IPerformanceCounterManager>();
            AckHandler = resolver.Resolve<IAckHandler>();
            ProtectedData = resolver.Resolve<IProtectedData>();
            UserIdProvider = resolver.Resolve<IUserIdProvider>();

            _configurationManager = resolver.Resolve<IConfigurationManager>();
            _transportManager = resolver.Resolve<ITransportManager>();

            // Ensure that this server is listening for any ACKs sent over the bus.
            resolver.Resolve<AckSubscriber>();

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

        protected JsonSerializer JsonSerializer { get; private set; }

        protected IAckHandler AckHandler { get; private set; }

        protected ITraceManager TraceManager { get; private set; }

        protected IPerformanceCounterManager Counters { get; private set; }

        protected ITransport Transport { get; private set; }

        protected IUserIdProvider UserIdProvider { get; private set; }

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
                return PrefixHelper.GetPersistentConnectionName(DefaultSignalRaw);
            }
        }

        private string DefaultSignalRaw
        {
            get
            {
                return GetType().FullName;
            }
        }

        internal virtual string GroupPrefix
        {
            get
            {
                return PrefixHelper.PersistentConnectionGroupPrefix;
            }
        }

        /// <summary>
        /// OWIN entry point.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public Task ProcessRequest(IDictionary<string, object> environment)
        {
            var context = new HostContext(environment);

            // Disable request compression and buffering on IIS
            environment.DisableRequestCompression();
            environment.DisableResponseBuffering();

            var response = new OwinResponse(environment);

            // Add the nosniff header for all responses to prevent IE from trying to sniff mime type from contents
            response.Headers.Set("X-Content-Type-Options", "nosniff");

            if (Authorize(context.Request))
            {
                return ProcessRequest(context);
            }

            if (context.Request.User != null &&
                context.Request.User.Identity.IsAuthenticated)
            {
                // If the user is authenticated and authorize failed then 403
                response.StatusCode = 403;
            }
            else
            {
                // If we failed to authorize the request then return a 401
                response.StatusCode = 401;
            }

            return TaskAsyncHelper.Empty;
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
                return FailResponse(context.Response, String.Format(CultureInfo.CurrentCulture, Resources.Error_ProtocolErrorUnknownTransport));
            }

            string connectionToken = context.Request.QueryString["connectionToken"];

            // If there's no connection id then this is a bad request
            if (String.IsNullOrEmpty(connectionToken))
            {
                return FailResponse(context.Response, String.Format(CultureInfo.CurrentCulture, Resources.Error_ProtocolErrorMissingConnectionToken));
            }

            string connectionId;
            string message;
            int statusCode;

            if (!TryGetConnectionId(context, connectionToken, out connectionId, out message, out statusCode))
            {
                return FailResponse(context.Response, message, statusCode);
            }

            // Set the transport's connection id to the unprotected one
            Transport.ConnectionId = connectionId;

            // Get the groups token from the request
            return Transport.GetGroupsToken()
                            .Then((g, pc, c) => pc.ProcessRequestPostGroupRead(c, g), this, context)
                            .FastUnwrap();
        }

        private Task ProcessRequestPostGroupRead(HostContext context, string groupsToken)
        {
            string connectionId = Transport.ConnectionId;

            // Get the user id from the request
            string userId = UserIdProvider.GetUserId(context.Request);

            IList<string> signals = GetSignals(userId, connectionId);
            IList<string> groups = AppendGroupPrefixes(context, connectionId, groupsToken);

            Connection connection = CreateConnection(connectionId, signals, groups);

            Connection = connection;
            string groupName = PrefixHelper.GetPersistentConnectionGroupName(DefaultSignalRaw);
            Groups = new GroupManager(connection, groupName);

            // We handle /start requests after the PersistentConnection has been initialized,
            // because ProcessStartRequest calls OnConnected.
            if (IsStartRequest(context.Request))
            {
                return ProcessStartRequest(context, connectionId);
            }

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

            Transport.Disconnected = clean =>
            {
                return TaskAsyncHelper.FromMethod(() => OnDisconnected(context.Request, connectionId, stopCalled: clean).OrEmpty());
            };

            return Transport.ProcessRequest(connection).OrEmpty().Catch(Trace, Counters.ErrorsAllTotal, Counters.ErrorsAllPerSec);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch any exception when unprotecting data.")]
        internal bool TryGetConnectionId(HostContext context,
                                           string connectionToken,
                                           out string connectionId,
                                           out string message,
                                           out int statusCode)
        {
            string unprotectedConnectionToken = null;

            // connectionId is only valid when this method returns true
            connectionId = null;

            // message and statusCode are only valid when this method returns false
            message = null;
            statusCode = 400;

            try
            {
                unprotectedConnectionToken = ProtectedData.Unprotect(connectionToken, Purposes.ConnectionToken);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Failed to process connectionToken {0}: {1}", connectionToken, ex);
            }

            if (String.IsNullOrEmpty(unprotectedConnectionToken))
            {
                message = String.Format(CultureInfo.CurrentCulture, Resources.Error_ConnectionIdIncorrectFormat);
                return false;
            }

            var tokens = unprotectedConnectionToken.Split(SplitChars, 2);

            connectionId = tokens[0];
            string tokenUserName = tokens.Length > 1 ? tokens[1] : String.Empty;
            string userName = GetUserIdentity(context);

            if (!String.Equals(tokenUserName, userName, StringComparison.OrdinalIgnoreCase))
            {
                message = String.Format(CultureInfo.CurrentCulture, Resources.Error_UnrecognizedUserIdentity);
                statusCode = 403;
                return false;
            }

            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to prevent any failures in unprotecting")]
        internal IList<string> VerifyGroups(string connectionId, string groupsToken)
        {
            if (String.IsNullOrEmpty(groupsToken))
            {
                return ListHelper<string>.Empty;
            }

            string unprotectedGroupsToken = null;

            try
            {
                unprotectedGroupsToken = ProtectedData.Unprotect(groupsToken, Purposes.Groups);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Failed to process groupsToken {0}: {1}", groupsToken, ex);
            }

            if (String.IsNullOrEmpty(unprotectedGroupsToken))
            {
                return ListHelper<string>.Empty;
            }

            var tokens = unprotectedGroupsToken.Split(SplitChars, 2);

            string groupConnectionId = tokens[0];
            string groupsValue = tokens.Length > 1 ? tokens[1] : String.Empty;

            if (!String.Equals(groupConnectionId, connectionId, StringComparison.OrdinalIgnoreCase))
            {
                return ListHelper<string>.Empty;
            }

            return JsonSerializer.Parse<string[]>(groupsValue);
        }

        private IList<string> AppendGroupPrefixes(HostContext context, string connectionId, string groupsToken)
        {
            return (from g in OnRejoiningGroups(context.Request, VerifyGroups(connectionId, groupsToken), connectionId)
                    select GroupPrefix + g).ToList();
        }

        private Connection CreateConnection(string connectionId, IList<string> signals, IList<string> groups)
        {
            return new Connection(MessageBus,
                                  JsonSerializer,
                                  DefaultSignal,
                                  connectionId,
                                  signals,
                                  groups,
                                  TraceManager,
                                  AckHandler,
                                  Counters,
                                  ProtectedData);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "This method is virtual and is used in the derived class")]
        private IList<string> GetDefaultSignals(string userId, string connectionId)
        {
            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)

            return new string[] {
                DefaultSignal,
                PrefixHelper.GetConnectionId(connectionId)
            };
        }

        /// <summary>
        /// Returns the signals used in the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <param name="userId">The user id for the current connection.</param>
        /// <param name="connectionId">The id of the incoming connection.</param>
        /// <returns>The signals used for this <see cref="PersistentConnection"/>.</returns>
        protected virtual IList<string> GetSignals(string userId, string connectionId)
        {
            return GetDefaultSignals(userId, connectionId);
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
        protected virtual IList<string> OnRejoiningGroups(IRequest request, IList<string> groups, string connectionId)
        {
            return groups;
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
        /// Called when a connection disconnects gracefully or due to a timeout.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the disconnected connection.</param>
        /// <param name="stopCalled">
        /// true, if stop was called on the client closing the connection gracefully;
        /// false, if the connection has been lost for longer than the
        /// <see cref="Configuration.IConfigurationManager.DisconnectTimeout"/>.
        /// Timeouts can occur in scaleout when clients reconnect with another server.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the disconnect operation is complete.</returns>
        protected virtual Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            return TaskAsyncHelper.Empty;
        }

        private static Task ProcessPingRequest(HostContext context)
        {
            return SendJsonResponse(context, PingJsonPayload);
        }

        private Task ProcessNegotiationRequest(HostContext context)
        {
            // Total amount of time without a keep alive before the client should attempt to reconnect in seconds.
            var keepAliveTimeout = _configurationManager.KeepAliveTimeout();
            string connectionId = Guid.NewGuid().ToString("d");
            string connectionToken = connectionId + ':' + GetUserIdentity(context);

            var payload = new
            {
                Url = context.Request.LocalPath.Replace("/negotiate", ""),
                ConnectionToken = ProtectedData.Protect(connectionToken, Purposes.ConnectionToken),
                ConnectionId = connectionId,
                KeepAliveTimeout = keepAliveTimeout != null ? keepAliveTimeout.Value.TotalSeconds : (double?)null,
                DisconnectTimeout = _configurationManager.DisconnectTimeout.TotalSeconds,
                ConnectionTimeout = _configurationManager.ConnectionTimeout.TotalSeconds,
                TryWebSockets = _transportManager.SupportsTransport(WebSocketsTransportName) && context.Environment.SupportsWebSockets(),
                ProtocolVersion = _protocolResolver.Resolve(context.Request).ToString(),
                TransportConnectTimeout = _configurationManager.TransportConnectTimeout.TotalSeconds,
                LongPollDelay = _configurationManager.LongPollDelay.TotalSeconds
            };

            return SendJsonResponse(context, JsonSerializer.Stringify(payload));
        }

        // Avoid async/await in SignalR 2.X due to https://github.com/SignalR/SignalR/issues/3116
        private Task ProcessStartRequest(HostContext context, string connectionId)
        {
            return OnConnected(context.Request, connectionId).OrEmpty()
                .Then(c => SendJsonResponse(c, StartJsonPayload), context)
                .Then(c => c.ConnectionsConnected.Increment(), Counters);
        }

        private static Task SendJsonResponse(HostContext context, string jsonPayload)
        {
            var callback = context.Request.QueryString["callback"];
            if (String.IsNullOrEmpty(callback))
            {
                // Send normal JSON response
                context.Response.ContentType = JsonUtility.JsonMimeType;
                return context.Response.End(jsonPayload);
            }

            // Send JSONP response since a callback is specified by the query string
            var callbackInvocation = JsonUtility.CreateJsonpCallback(callback, jsonPayload);
            context.Response.ContentType = JsonUtility.JavaScriptMimeType;
            return context.Response.End(callbackInvocation);
        }

        private static string GetUserIdentity(HostContext context)
        {
            if (context.Request.User != null && context.Request.User.Identity.IsAuthenticated)
            {
                return context.Request.User.Identity.Name ?? String.Empty;
            }
            return String.Empty;
        }

        private static Task FailResponse(IResponse response, string message, int statusCode = 400)
        {
            response.StatusCode = statusCode;
            return response.End(message);
        }

        private static bool IsNegotiationRequest(IRequest request)
        {
            return request.LocalPath.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStartRequest(IRequest request)
        {
            return request.LocalPath.EndsWith("/start", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPingRequest(IRequest request)
        {
            return request.LocalPath.EndsWith("/ping", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HostContext context)
        {
            return _transportManager.GetTransport(context);
        }
    }
}
