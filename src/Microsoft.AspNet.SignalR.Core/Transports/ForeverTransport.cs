// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "The disposer is an optimization")]
    public abstract class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private static readonly ProtocolResolver _protocolResolver = new ProtocolResolver();

        private readonly IPerformanceCounterManager _counters;
        private readonly JsonSerializer _jsonSerializer;
        private IDisposable _busRegistration;

        internal RequestLifetime _transportLifetime;

        protected ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<JsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {
        }

        protected ForeverTransport(HostContext context,
                                   JsonSerializer jsonSerializer,
                                   ITransportHeartbeat heartbeat,
                                   IPerformanceCounterManager performanceCounterManager,
                                   ITraceManager traceManager)
            : base(context, heartbeat, performanceCounterManager, traceManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterManager;
        }

        protected virtual int MaxMessages
        {
            get
            {
                return 10;
            }
        }

        protected JsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected virtual void OnSending(string payload)
        {
            Heartbeat.MarkConnection(this);
        }

        protected virtual void OnSendingResponse(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        // Unit testing hooks
        internal Action AfterReceive;
        internal Action BeforeCancellationTokenCallbackRegistered;
        internal Action BeforeReceive;
        internal Action<Exception> AfterRequestEnd;

        protected override Task InitializePersistentState()
        {
            return base.InitializePersistentState().Then(t =>
            {
                // The _transportLifetime must be initialized after calling base.InitializePersistentState since
                // _transportLifetime depends on _requestLifetime.
                t._transportLifetime = new RequestLifetime(t, t._requestLifeTime);
            }, this);
        }

        protected Task ProcessRequestCore(ITransportConnection connection)
        {
            Connection = connection;

            if (IsSendRequest)
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort(ConnectionId);
            }
            else
            {
                return InitializePersistentState()
                    .Then((t, c) => t.ProcessReceiveRequest(c), this, connection);
            }
        }

        public virtual Task ProcessRequest(ITransportConnection connection)
        {
            return ProcessRequestCore(connection);
        }

        public abstract Task Send(PersistentResponse response);

        public virtual Task Send(object value)
        {
            var context = new ForeverTransportContext(this, value);

            return EnqueueOperation(state => PerformSend(state), context);
        }

        protected internal virtual Task InitializeResponse(ITransportConnection connection)
        {
            return TaskAsyncHelper.Empty;
        }

        protected void OnError(Exception ex)
        {
            IncrementErrors();

            // Complete the http request
            _transportLifetime.Complete(ex);
        }

        protected virtual async Task ProcessSendRequest()
        {
            INameValueCollection form = await Context.Request.ReadForm().PreserveCulture();
            string data = form["data"];

            if (Received != null)
            {
                await Received(data).PreserveCulture();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private Task ProcessReceiveRequest(ITransportConnection connection)
        {
            Func<Task> initialize = null;

            // If this transport isn't replacing an existing transport, oldConnection will be null.
            ITrackingConnection oldConnection = Heartbeat.AddOrUpdateConnection(this);
            bool newConnection = oldConnection == null;

            if (IsConnectRequest)
            {
                if (_protocolResolver.SupportsDelayedStart(Context.Request))
                {
                    // TODO: Ensure delegate continues to use the C# Compiler static delegate caching optimization. 
                    initialize = () => connection.Initialize(ConnectionId);
                }
                else
                {
                    Func<Task> connected;
                    if (newConnection)
                    {
                        connected = Connected ?? _emptyTaskFunc;
                        _counters.ConnectionsConnected.Increment();
                    }
                    else
                    {
                        // Wait until the previous call to Connected completes.
                        // We don't want to call Connected twice
                        connected = () => oldConnection.ConnectTask;
                    }

                    initialize = () =>
                    {
                        return connected().Then((conn, id) => conn.Initialize(id), connection, ConnectionId);
                    };
                }
            }
            else if (!SuppressReconnect)
            {
                initialize = Reconnected;
                _counters.ConnectionsReconnected.Increment();
            }

            initialize = initialize ?? _emptyTaskFunc;

            Func<Task> fullInit = () => initialize().ContinueWith(_connectTcs);

            return ProcessMessages(connection, fullInit);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object is disposed otherwise")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private Task ProcessMessages(ITransportConnection connection, Func<Task> initialize)
        {
            var disposer = new Disposer();

            if (BeforeCancellationTokenCallbackRegistered != null)
            {
                BeforeCancellationTokenCallbackRegistered();
            }

            var cancelContext = new ForeverTransportContext(this, disposer);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            _busRegistration = ConnectionEndToken.SafeRegister(state => Cancel(state), cancelContext);

            if (BeforeReceive != null)
            {
                BeforeReceive();
            }

            try
            {
                // Ensure we enqueue the response initialization before any messages are received
                EnqueueOperation(state => InitializeResponse((ITransportConnection)state), connection)
                    .Catch((ex, state) => ((ForeverTransport)state).OnError(ex), this, Trace);

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                IDisposable subscription = connection.Receive(LastMessageId,
                                                              (response, state) => ((ForeverTransport)state).OnMessageReceived(response),
                                                               MaxMessages,
                                                               this);

                if (AfterReceive != null)
                {
                    AfterReceive();
                }

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                initialize().Catch((ex, state) => ((ForeverTransport)state).OnError(ex), this, Trace)
                            .Finally(state => ((SubscriptionDisposerContext)state).Set(),
                                new SubscriptionDisposerContext(disposer, subscription));
            }
            catch (Exception ex)
            {
                _transportLifetime.Complete(ex);
            }

            return _requestLifeTime.Task;
        }

        private static void Cancel(object state)
        {
            var context = (ForeverTransportContext)state;

            context.Transport.Trace.TraceEvent(TraceEventType.Verbose, 0, "Cancel(" + context.Transport.ConnectionId + ")");

            ((IDisposable)context.State).Dispose();
        }

        protected virtual Task<bool> OnMessageReceived(PersistentResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            response.Reconnect = HostShutdownToken.IsCancellationRequested;

            if (IsTimedOut || response.Aborted)
            {
                _busRegistration.Dispose();

                if (response.Aborted)
                {
                    // If this was a clean disconnect raise the event.
                    return Abort().Then(() => TaskAsyncHelper.False);
                }
            }

            if (response.Terminal)
            {
                // End the request on the terminal response
                _transportLifetime.Complete();

                return TaskAsyncHelper.False;
            }

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return Send(response).Then(() => TaskAsyncHelper.True);
        }

        private static Task PerformSend(object state)
        {
            var context = (ForeverTransportContext)state;

            if (!context.Transport.IsAlive)
            {
                return TaskAsyncHelper.Empty;
            }

            context.Transport.Context.Response.ContentType = JsonUtility.JsonMimeType;

            context.Transport.JsonSerializer.Serialize(context.State, context.Transport.OutputWriter);
            context.Transport.OutputWriter.Flush();

            return TaskAsyncHelper.Empty;
        }

        private class ForeverTransportContext
        {
            public object State;
            public ForeverTransport Transport;

            public ForeverTransportContext(ForeverTransport foreverTransport, object state)
            {
                State = state;
                Transport = foreverTransport;
            }
        }

        private class SubscriptionDisposerContext
        {
            private readonly Disposer _disposer;
            private readonly IDisposable _supscription;

            public SubscriptionDisposerContext(Disposer disposer, IDisposable subscription)
            {
                _disposer = disposer;
                _supscription = subscription;
            }

            public void Set()
            {
                _disposer.Set(_supscription);
            }
        }

        internal class RequestLifetime
        {
            private readonly HttpRequestLifeTime _lifetime;
            private readonly ForeverTransport _transport;

            public RequestLifetime(ForeverTransport transport, HttpRequestLifeTime lifetime)
            {
                _lifetime = lifetime;
                _transport = transport;
            }

            public void Complete()
            {
                Complete(error: null);
            }

            public void Complete(Exception error)
            {
                _lifetime.Complete(error);

                _transport.Dispose();

                if (_transport.AfterRequestEnd != null)
                {
                    _transport.AfterRequestEnd(error);
                }
            }
        }
    }
}
