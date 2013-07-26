﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class LongPollingTransport : TransportDisconnectBase, ITransport
    {
        private readonly JsonSerializer _jsonSerializer;
        private readonly IPerformanceCounterManager _counters;
        private readonly IConfigurationManager _configurationManager;

        // This should be ok to do since long polling request never hang around too long
        // so we won't bloat memory
        private const int MaxMessages = 5000;

        public LongPollingTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<JsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>(),
                   resolver.Resolve<IConfigurationManager>())
        {

        }

        public LongPollingTransport(HostContext context,
                                    JsonSerializer jsonSerializer,
                                    ITransportHeartbeat heartbeat,
                                    IPerformanceCounterManager performanceCounterManager,
                                    ITraceManager traceManager,
                                    IConfigurationManager configurationManager)
            : base(context, heartbeat, performanceCounterManager, traceManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterManager;
            _configurationManager = configurationManager;
        }

        public override TimeSpan DisconnectThreshold
        {
            get { return _configurationManager.LongPollDelay; }
        }

        public override bool IsConnectRequest
        {
            get
            {
                return Context.Request.LocalPath.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsReconnectRequest
        {
            get
            {
                return Context.Request.LocalPath.EndsWith("/reconnect", StringComparison.OrdinalIgnoreCase);
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
                return Context.Request.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
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

                return ProcessReceiveRequest(connection);
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
            var context = new LongPollingTransportContext(this, value);

            return EnqueueOperation(state => PerformSend(state), context);
        }

        private async Task ProcessSendRequest()
        {
            INameValueCollection form = await Context.Request.ReadForm();

            string data = form["data"] ?? Context.Request.QueryString["data"];

            if (Received != null)
            {
                await Received(data);
            }
        }

        private Task ProcessReceiveRequest(ITransportConnection connection)
        {
            Func<Task> initialize = null;

            // If this transport isn't replacing an existing transport, oldConnection will be null.
            ITrackingConnection oldConnection = Heartbeat.AddOrUpdateConnection(this);
            bool newConnection = oldConnection == null;

            if (IsConnectRequest)
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
            else if (IsReconnectRequest)
            {
                initialize = Reconnected;
            }

            var series = new Func<object, Task>[] 
            { 
                state => ((Func<Task>)state).Invoke(),
                state => ((Func<Task>)state).Invoke()
            };

            var states = new object[] { TransportConnected ?? _emptyTaskFunc, 
                                        initialize ?? _emptyTaskFunc };

            Func<Task> fullInit = () => TaskAsyncHelper.Series(series, states).ContinueWith(_connectTcs);

            return ProcessMessages(connection, fullInit);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The subscription is disposed in the callback")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is captured in a task")]
        private Task ProcessMessages(ITransportConnection connection, Func<Task> initialize)
        {
            var disposer = new Disposer();

            var cancelContext = new LongPollingTransportContext(this, disposer);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            IDisposable registration = ConnectionEndToken.SafeRegister(state => Cancel(state), cancelContext);

            var lifeTime = new RequestLifetime(this, _requestLifeTime, registration);
            var messageContext = new MessageContext(this, lifeTime);

            try
            {
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
                lifeTime.Complete(ex);
            }

            return _requestLifeTime.Task;
        }

        private static void Cancel(object state)
        {
            var context = (LongPollingTransportContext)state;

            context.Transport.Trace.TraceEvent(TraceEventType.Verbose, 0, "Cancel(" + context.Transport.ConnectionId + ")");

            ((IDisposable)context.State).Dispose();
        }

        private static Task<bool> OnMessageReceived(PersistentResponse response, object state)
        {
            var context = (MessageContext)state;

            response.Reconnect = context.Transport.HostShutdownToken.IsCancellationRequested;

            Task task = TaskAsyncHelper.Empty;

            if (response.Aborted)
            {
                // If this was a clean disconnect then raise the event
                task = context.Transport.Abort();
            }

            if (response.Terminal)
            {
                // If the response wasn't sent, send it before ending the request
                if (!context.ResponseSent)
                {
                    // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                    return task.Then((ctx, resp) => ctx.Transport.Send(resp), context, response)
                               .Then(() =>
                               {
                                   context.Lifetime.Complete();

                                   return TaskAsyncHelper.False;
                               });
                }

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                return task.Then(() =>
                {
                    context.Lifetime.Complete();

                    return TaskAsyncHelper.False;
                });
            }

            // Mark the response as sent
            context.ResponseSent = true;

            // Send the response and return false
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return task.Then((ctx, resp) => ctx.Transport.Send(resp), context, response)
                       .Then(() => TaskAsyncHelper.False);
        }

        private static Task PerformSend(object state)
        {
            var context = (LongPollingTransportContext)state;

            if (!context.Transport.IsAlive)
            {
                return TaskAsyncHelper.Empty;
            }

            context.Transport.Context.Response.ContentType = context.Transport.IsJsonp ? JsonUtility.JavaScriptMimeType : JsonUtility.JsonMimeType; 

            if (context.Transport.IsJsonp)
            {
                context.Transport.OutputWriter.Write(context.Transport.JsonpCallback);
                context.Transport.OutputWriter.Write("(");
            }

            context.Transport._jsonSerializer.Serialize(context.State, context.Transport.OutputWriter);

            if (context.Transport.IsJsonp)
            {
                context.Transport.OutputWriter.Write(");");
            }

            context.Transport.OutputWriter.Flush();

            return TaskAsyncHelper.Empty;
        }

        private static void OnError(AggregateException ex, object state)
        {
            var context = (MessageContext)state;

            context.Transport.IncrementErrors();

            context.Lifetime.Complete(ex);
        }

        private void AddTransportData(PersistentResponse response)
        {
            if (_configurationManager.LongPollDelay != TimeSpan.Zero)
            {
                response.LongPollDelay = (long)_configurationManager.LongPollDelay.TotalMilliseconds;
            }
        }

        private class LongPollingTransportContext
        {
            public object State;
            public LongPollingTransport Transport;

            public LongPollingTransportContext(LongPollingTransport transport, object state)
            {
                State = state;
                Transport = transport;
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
            private readonly LongPollingTransport _transport;
            private readonly IDisposable _registration;

            public RequestLifetime(LongPollingTransport transport, HttpRequestLifeTime requestLifeTime, IDisposable registration)
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
