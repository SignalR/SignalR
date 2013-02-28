// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Transports
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "The disposer is an optimization")]
    public abstract class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private readonly IPerformanceCounterManager _counters;
        private IJsonSerializer _jsonSerializer;
        private string _lastMessageId;
        private Disposer _requestDisposer;

        private const int MaxMessages = 10;

        protected ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {
        }

        protected ForeverTransport(HostContext context,
                                   IJsonSerializer jsonSerializer,
                                   ITransportHeartbeat heartbeat,
                                   IPerformanceCounterManager performanceCounterWriter,
                                   ITraceManager traceManager)
            : base(context, heartbeat, performanceCounterWriter, traceManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterWriter;
        }

        protected string LastMessageId
        {
            get
            {
                if (_lastMessageId == null)
                {
                    _lastMessageId = Context.Request.QueryString["messageId"];
                }

                return _lastMessageId;
            }
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        internal TaskCompletionSource<object> InitializeTcs { get; set; }

        protected virtual void OnSending(string payload)
        {
            Heartbeat.MarkConnection(this);
        }

        protected virtual void OnSendingResponse(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        // Unit testing hooks
        internal Action AfterReceive;
        internal Action BeforeCancellationTokenCallbackRegistered;
        internal Action BeforeReceive;
        internal Action<Exception> AfterRequestEnd;

        protected override void InitializePersistentState()
        {
            // PersistentConnection.OnConnected must complete before we can write to the output stream,
            // so clients don't indicate the connection has started too early.
            InitializeTcs = new TaskCompletionSource<object>();
            WriteQueue = new TaskQueue(InitializeTcs.Task);

            _requestDisposer = new Disposer();

            base.InitializePersistentState();
        }

        protected Task ProcessRequestCore(ITransportConnection connection)
        {
            Connection = connection;

            if (Context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort(ConnectionId);
            }
            else
            {
                InitializePersistentState();

                if (IsConnectRequest)
                {
                    if (Connected != null)
                    {
                        // Return a task that completes when the connected event task & the receive loop task are both finished
                        bool newConnection = Heartbeat.AddConnection(this);

                        // The connected callback
                        Func<Task> connected = () =>
                        {
                            if (newConnection)
                            {
                                return Connected().Then(() => _counters.ConnectionsConnected.Increment());
                            }
                            return TaskAsyncHelper.Empty;
                        };

                        return TaskAsyncHelper.Interleave(ProcessReceiveRequestWithoutTracking, connected, connection, Completed);
                    }

                    return ProcessReceiveRequest(connection);
                }

                if (Reconnected != null)
                {
                    // Return a task that completes when the reconnected event task & the receive loop task are both finished
                    Func<Task> reconnected = () => Reconnected().Then(() => _counters.ConnectionsReconnected.Increment());
                    return TaskAsyncHelper.Interleave(ProcessReceiveRequest, reconnected, connection, Completed);
                }

                return ProcessReceiveRequest(connection);
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

        protected internal override Task EnqueueOperation(Func<object, Task> writeAsync, object state)
        {
            Task task = base.EnqueueOperation(writeAsync, state);

            // If PersistentConnection.OnConnected has not completed (as indicated by InitializeTcs),
            // the queue will be blocked to prevent clients from prematurely indicating the connection has
            // started, but we must keep receive loop running to continue processing commands and to
            // prevent deadlocks caused by waiting on ACKs.
            if (InitializeTcs == null || InitializeTcs.Task.IsCompleted)
            {
                return task;
            }

            return TaskAsyncHelper.Empty;
        }

        protected override void ReleaseRequest()
        {
            _requestDisposer.Dispose();
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Func<Task> postReceive = null)
        {
            Heartbeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Func<Task> postReceive = null)
        {
            postReceive = postReceive ?? new Func<Task>(() => TaskAsyncHelper.Empty);

            Func<Task> afterReceive = () =>
            {
                var series = new Func<object, Task>[] 
                { 
                    OnTransportConnected,
                    state => InitializeResponse((ITransportConnection)state),
                    state => ((Func<Task>)state).Invoke()
                };

                return TaskAsyncHelper.Series(series, new object[] { null, connection, postReceive });
            };

            return ProcessMessages(connection, afterReceive);
        }

        private Task OnTransportConnected(object state)
        {
            if (TransportConnected != null)
            {
                return TransportConnected().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec);
            }

            return TaskAsyncHelper.Empty;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object is disposed otherwise")]
        private Task ProcessMessages(ITransportConnection connection, Func<Task> postReceive)
        {
            var processMessagesTcs = new TaskCompletionSource<object>();

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            var lifetime = new RequestLifetime(this, processMessagesTcs);

            var disposable = new DisposableAction(state => ((RequestLifetime)state).Complete(),
                                                  lifetime);

            _requestDisposer.Set(disposable);

            ProcessMessages(connection, postReceive, lifetime);

            return processMessagesTcs.Task;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object is disposed otherwise")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private void ProcessMessages(ITransportConnection connection, Func<Task> postReceive, RequestLifetime lifetime)
        {
            var disposer = new Disposer();
            
            if (BeforeCancellationTokenCallbackRegistered != null)
            {
                BeforeCancellationTokenCallbackRegistered();
            }

            var cancelContext = new ForeverTransportContext(this, disposer);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            IDisposable registration = ConnectionEndToken.SafeRegister(state => Cancel(state), cancelContext);

            var messageContext = new MessageContext(registration, lifetime, this);

            if (BeforeReceive != null)
            {
                BeforeReceive();
            }

            try
            {
                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                IDisposable subscription = connection.Receive(LastMessageId,
                                                              (response, state) => OnMessageReceived(response, state),
                                                               MaxMessages,
                                                               messageContext);


                disposer.Set(subscription);
            }
            catch (Exception ex)
            {
                // Set the tcs so that the task queue isn't waiting forever
                InitializeTcs.TrySetResult(null);

                lifetime.Complete(ex);

                return;
            }

            if (AfterReceive != null)
            {
                AfterReceive();
            }

            var errorContext = new ForeverTransportContext(this, lifetime);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            postReceive().Catch((ex, state) => OnPostReceiveError(ex, state), errorContext)
                         .ContinueWith(InitializeTcs);
        }

        private static void Cancel(object state)
        {
            var context = (ForeverTransportContext)state;

            context.Transport.Trace.TraceEvent(TraceEventType.Verbose, 0, "Cancel(" + context.Transport.ConnectionId + ")");

            ((IDisposable)context.State).Dispose();
        }

        private static Task<bool> OnMessageReceived(PersistentResponse response, object state)
        {
            var context = (MessageContext)state;

            response.TimedOut = context.Transport.IsTimedOut;

            // If we're telling the client to disconnect then clean up the instantiated connection.
            if (response.Disconnect)
            {
                // Send the response before removing any connection data
                return context.Transport.Send(response).Then(c => OnDisconnectMessage(c), context)
                                        .Then(() => TaskAsyncHelper.False);
            }
            else if (response.TimedOut || response.Aborted)
            {
                context.Registration.Dispose();

                if (response.Aborted)
                {
                    // If this was a clean disconnect raise the event.
                    return context.Transport.OnDisconnect()
                                            .Then(() => TaskAsyncHelper.False);
                }
            }
            
            if (response.Terminal)
            {
                // End the request on the terminal response
                context.Lifetime.Complete();

                return TaskAsyncHelper.False;
            }

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return context.Transport.Send(response).Then(() => TaskAsyncHelper.True)
                                                   .Catch((ex, s) => OnSendError(ex, s), context.Transport);
        }

        private static void OnDisconnectMessage(MessageContext context)
        {
            context.Registration.Dispose();

            // Remove connection without triggering disconnect
            context.Transport.Heartbeat.RemoveConnection(context.Transport);
        }

        private static Task PerformSend(object state)
        {
            var context = (ForeverTransportContext)state;

            context.Transport.Context.Response.ContentType = JsonUtility.JsonMimeType;

            context.Transport.JsonSerializer.Serialize(context.State, context.Transport.OutputWriter);
            context.Transport.OutputWriter.Flush();

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return context.Transport.Context.Response.End().Catch((ex, s) => OnSendError(ex, s), context.Transport);
        }

        private static void OnSendError(AggregateException ex, object state)
        {
            var transport = (ForeverTransport)state;

            transport.IncrementErrors(ex);

            transport.Trace.TraceEvent(TraceEventType.Error, 0, "Send failed for {0} with: {1}", transport.ConnectionId, ex.GetBaseException());
        }

        private static void OnPostReceiveError(AggregateException ex, object state)
        {
            var context = (ForeverTransportContext)state;

            context.Transport.Trace.TraceEvent(TraceEventType.Error, 0, "Failed post receive for {0} with: {1}", context.Transport.ConnectionId, ex.GetBaseException());

            ((RequestLifetime)context.State).Complete(ex);

            context.Transport._counters.ErrorsAllTotal.Increment();
            context.Transport._counters.ErrorsAllPerSec.Increment();
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

        private class MessageContext
        {
            public IDisposable Registration;
            public RequestLifetime Lifetime;
            public ForeverTransport Transport;

            public MessageContext(IDisposable registration, RequestLifetime lifetime, ForeverTransport transport)
            {
                Registration = registration;
                Lifetime = lifetime;
                Transport = transport;
            }
        }

        private class RequestLifetime
        {
            private readonly TaskCompletionSource<object> _lifetimeTcs;
            private readonly ForeverTransport _transport;

            public RequestLifetime(ForeverTransport transport, TaskCompletionSource<object> lifetimeTcs)
            {
                _lifetimeTcs = lifetimeTcs;
                _transport = transport;
            }

            public void Complete()
            {
                Complete(error: null);
            }

            public void Complete(Exception error)
            {
                _transport.Trace.TraceEvent(TraceEventType.Verbose, 0, "DrainWrites(" + _transport.ConnectionId + ")");

                var context = new DrainContext(_lifetimeTcs, error);

                // Drain the task queue for pending write operations so we don't end the request and then try to write
                // to a corrupted request object.
                _transport.WriteQueue.Drain().Catch().Finally(state =>
                {
                    // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                    ((DrainContext)state).Complete();
                },
                context);

                _transport.CompleteRequest();

                _transport.Trace.TraceInformation("EndRequest(" + _transport.ConnectionId + ")");

                _transport.Dispose();

                if (_transport.AfterRequestEnd != null)
                {
                    _transport.AfterRequestEnd(error);
                }
            }

            private class DrainContext
            {
                private readonly TaskCompletionSource<object> _lifetimeTcs;
                private readonly Exception _error;

                public DrainContext(TaskCompletionSource<object> lifetimeTcs, Exception error)
                {
                    _lifetimeTcs = lifetimeTcs;
                    _error = error;
                }

                public void Complete()
                {
                    if (_error != null)
                    {
                        _lifetimeTcs.TrySetException(_error);
                    }
                    else
                    {
                        _lifetimeTcs.TrySetResult(null);
                    }
                }
            }
        }
    }
}
