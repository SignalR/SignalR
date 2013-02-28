// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class LongPollingTransport : TransportDisconnectBase, ITransport
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IPerformanceCounterManager _counters;

        // This should be ok to do since long polling request never hang around too long
        // so we won't bloat memory
        private const int MaxMessages = 5000;

        public LongPollingTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {

        }

        public LongPollingTransport(HostContext context,
                                    IJsonSerializer jsonSerializer,
                                    ITransportHeartbeat heartbeat,
                                    IPerformanceCounterManager performanceCounterManager,
                                    ITraceManager traceManager)
            : base(context, heartbeat, performanceCounterManager, traceManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterManager;
        }

        /// <summary>
        /// The number of milliseconds to tell the browser to wait before restablishing a
        /// long poll connection after data is sent from the server. Defaults to 0.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long", Justification = "Longpolling is a well known term")]
        public static long LongPollDelay
        {
            get;
            set;
        }

        public override TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromMilliseconds(LongPollDelay); }
        }

        public override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsReconnectRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/reconnect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsJsonp
        {
            get
            {
                return !String.IsNullOrEmpty(JsonpCallback);
            }
        }

        private bool IsSendRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
            }
        }

        private string MessageId
        {
            get
            {
                return Context.Request.QueryString["messageId"];
            }
        }

        private string JsonpCallback
        {
            get
            {
                return Context.Request.QueryString["callback"];
            }
        }

        public override bool SupportsKeepAlive
        {
            get
            {
                return false;
            }
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public Task ProcessRequest(ITransportConnection connection)
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
                InitializePersistentState();

                Func<Task> initialize = _emptyTaskFunc;

                if (IsConnectRequest)
                {
                    initialize = Connected ?? _emptyTaskFunc;

                    // TODO: Re-enable this
                    // We're going to raise connect multiple times if we're falling back
                    // _counters.ConnectionsConnected.Increment();
                }
                else if(IsReconnectRequest)
                {
                    initialize = Reconnected ?? _emptyTaskFunc;
                }

                return ProcessReceiveRequest(connection, initialize);
            }
        }

        public Task Send(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);

            AddTransportData(response);

            return Send((object)response);
        }

        public Task Send(object value)
        {
            return EnqueueOperation(state =>
            {
                Context.Response.ContentType = IsJsonp ? JsonUtility.JavaScriptMimeType : JsonUtility.JsonMimeType;

                if (IsJsonp)
                {
                    OutputWriter.Write(JsonpCallback);
                    OutputWriter.Write("(");
                }

                _jsonSerializer.Serialize(state, OutputWriter);

                if (IsJsonp)
                {
                    OutputWriter.Write(");");
                }

                OutputWriter.Flush();
                return Context.Response.End();
            },
            value);
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"] ?? Context.Request.QueryString["data"];

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Func<Task> initialize)
        {
            Heartbeat.AddConnection(this);

            var series = new Func<object, Task>[] 
            { 
                state => ((Func<Task>)state ?? _emptyTaskFunc).Invoke(),
                state => ((Func<Task>)state).Invoke()
            };

            var states = new object[] { TransportConnected, initialize };

            Func<Task> fullInit = () => TaskAsyncHelper.Series(series, states);

            return ProcessMessages(connection, fullInit);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The subscription is disposed in the callback")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is captured in a task")]
        private Task ProcessMessages(ITransportConnection connection, Func<Task> initialize)
        {
            var disposer = new Disposer();

            IDisposable registration = ConnectionEndToken.SafeRegister(state =>
            {
                ((IDisposable)state).Dispose();
            },
            disposer);

            var requestLifetime = new RequestLifetime(this, registration, _requestLifeTime);

            try
            {                
                var messageContext = new MessageContext(this, requestLifetime);

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                IDisposable subscription = connection.Receive(MessageId,
                                                              (response, state) => OnMessageReceived(response, state),
                                                              MaxMessages,
                                                              messageContext);

                // Set the disposable
                disposer.Set(subscription);

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                initialize().Catch((ex, state) => OnError(ex, state), messageContext);
            }
            catch (Exception ex)
            {
                requestLifetime.Complete(ex);
            }

            return _requestLifeTime.Task;
        }

        private static Task<bool> OnMessageReceived(PersistentResponse response, object state)
        {
            var context = (MessageContext)state;
            var requestLifetime = (RequestLifetime)context.Lifetime;

            response.TimedOut = context.Transport.IsTimedOut;

            Task task = TaskAsyncHelper.Empty;

            if (response.Aborted)
            {
                // If this was a clean disconnect then raise the event
                task = context.Transport.OnDisconnect();
            }

            if (response.Terminal)
            {
                // If the response wasn't sent, send it before ending the request
                if (!context.ResponseSent)
                {
                    // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                    return task.Then((ctx, resp) => ctx.Transport.Send(resp), context, response)
                               .Catch((ex, s) => OnError(ex, s), context)
                               .Then(() =>
                               {
                                   requestLifetime.Complete();

                                   return TaskAsyncHelper.False;
                               });
                }

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                return task.Catch((ex, s) => OnError(ex, s), context)
                           .Then(() =>
                           {
                               requestLifetime.Complete();

                               return TaskAsyncHelper.False;
                           });
            }

            // Mark the response as sent
            context.ResponseSent = true;

            // Send the response and return false
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return task.Then((ctx, resp) => ctx.Transport.Send(resp), context, response)
                       .Catch((ex, s) => OnError(ex, s), context)
                       .Then(() => TaskAsyncHelper.False);
        }

        private static void OnError(AggregateException ex, object state)
        {
            var context = (MessageContext)state;

            context.Transport.IncrementErrors(ex);

            context.Transport.Trace.TraceEvent(TraceEventType.Error, 0, "Error on connection {0} with: {1}", context.Transport.ConnectionId, ex.GetBaseException());

            context.Lifetime.Complete(ex);

            context.Transport._counters.ErrorsAllTotal.Increment();
            context.Transport._counters.ErrorsAllPerSec.Increment();
        }

        private static void AddTransportData(PersistentResponse response)
        {
            if (LongPollDelay > 0)
            {
                response.LongPollDelay = LongPollDelay;
            }
        }

        private class MessageContext
        {
            public LongPollingTransport Transport;
            public RequestLifetime Lifetime;
            public bool ResponseSent;

            public MessageContext(LongPollingTransport longPollingTransport, RequestLifetime requestLifetime)
            {
                Transport = longPollingTransport;
                Lifetime = requestLifetime;
            }
        }

        private class RequestLifetime
        {
            private readonly HttpRequestLifeTime _requestLifeTime;
            private readonly IDisposable _registration;
            private readonly LongPollingTransport _transport;

            public RequestLifetime(LongPollingTransport transport, IDisposable registration, HttpRequestLifeTime requestLifeTime)
            {
                _transport = transport;
                _registration = registration;
                _requestLifeTime = requestLifeTime;
            }

            public void Complete()
            {
                Complete(exception: null);
            }

            public void Complete(Exception exception)
            {
                // End the request
                _requestLifeTime.Complete(exception);

                // Dispose of the cancellation token subscription
                _registration.Dispose();
                
                // Dispose any state on the transport
                _transport.Dispose();
            }
        }
    }
}
