// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class LongPollingTransport : ForeverTransport, ITransport
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly IPerformanceCounterManager _counters;
        private bool _responseSent;

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
            : base(context, jsonSerializer, heartbeat, performanceCounterManager, traceManager)
        {
            _configurationManager = configurationManager;
            _counters = performanceCounterManager;
        }

        public override TimeSpan DisconnectThreshold
        {
            get { return _configurationManager.LongPollDelay; }
        }

        private bool IsJsonp
        {
            get
            {
                return !String.IsNullOrEmpty(JsonpCallback);
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
                return !IsJsonp;
            }
        }

        public override bool RequiresTimeout
        {
            get
            {
                return true;
            }
        }

        // This should be ok to do since long polling request never hang around too long
        // so we won't bloat memory
        protected override int MaxMessages
        {
            get
            {
                return 5000;
            }
        }

        protected override bool SuppressReconnect
        {
            get
            {
                return !Context.Request.LocalPath.EndsWith("/reconnect", StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override Task InitializeMessageId()
        {
            _lastMessageId = Context.Request.QueryString["messageId"];

            if (_lastMessageId == null)
            {
                return Context.Request.ReadForm().Then((form, t) =>
                {
                    t._lastMessageId = form["messageId"];
                }, this);
            }

            return TaskAsyncHelper.Empty;
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is for async.")]
        public override Task<string> GetGroupsToken()
        {
            var groupsToken = Context.Request.QueryString["groupsToken"];

            if (groupsToken == null)
            {
                return Context.Request.ReadForm().Then(form =>
                {
                    return form["groupsToken"];
                });
            }

            return Task.FromResult(groupsToken);
        }

        public override Task KeepAlive()
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformKeepAlive(state), this);
        }

        public override Task Send(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);

            AddTransportData(response);

            // This overload is only used in response to /connect, /poll and /reconnect requests,
            // so the response will have already been initialized by ProcessMessages.
            var context = new LongPollingTransportContext(this, response);
            return EnqueueOperation(state => PerformPartialSend(state), context);
        }

        public override Task Send(object value)
        {
            var context = new LongPollingTransportContext(this, value);

            // This overload is only used in response to /send requests,
            // so the response will be uninitialized.
            return EnqueueOperation(state => PerformCompleteSend(state), context);
        }

        public override void IncrementConnectionsCount()
        {
            _counters.ConnectionsCurrentLongPolling.Increment();
        }
        
        public override void DecrementConnectionsCount()
        {
            _counters.ConnectionsCurrentLongPolling.Decrement();
        }

        protected override Task<bool> OnMessageReceived(PersistentResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            response.Reconnect = HostShutdownToken.IsCancellationRequested;

            Task task = TaskAsyncHelper.Empty;

            if (response.Aborted)
            {
                // If this was a clean disconnect then raise the event
                task = Abort();
            }

            if (response.Terminal)
            {
                // If the response wasn't sent, send it before ending the request
                if (!_responseSent)
                {
                    // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                    return task.Then((transport, resp) => transport.Send(resp), this, response)
                               .Then(() =>
                               {
                                   _transportLifetime.Complete();

                                   return TaskAsyncHelper.False;
                               });
                }

                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                return task.Then(() =>
                {
                    _transportLifetime.Complete();

                    return TaskAsyncHelper.False;
                });
            }

            // Mark the response as sent
            _responseSent = true;

            // Send the response and return false
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return task.Then((transport, resp) => transport.Send(resp), this, response)
                       .Then(() => TaskAsyncHelper.False);
        }

        protected internal override Task InitializeResponse(ITransportConnection connection)
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return base.InitializeResponse(connection)
                       .Then(s => WriteInit(s), this);
        }

        protected override async Task ProcessSendRequest()
        {
            INameValueCollection form = await Context.Request.ReadForm().PreserveCulture();
            string data = form["data"] ?? Context.Request.QueryString["data"];

            if (Received != null)
            {
                await Received(data).PreserveCulture();
            }
        }

        private static Task WriteInit(LongPollingTransport transport)
        {
            transport.Context.Response.ContentType = transport.IsJsonp ? JsonUtility.JavaScriptMimeType : JsonUtility.JsonMimeType;
            return transport.Context.Response.Flush();
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (LongPollingTransport)state;

            if (!transport.IsAlive)
            {
                return TaskAsyncHelper.Empty;
            }

            transport.OutputWriter.Write(' ');
            transport.OutputWriter.Flush();

            return transport.Context.Response.Flush();
        }

        private static Task PerformPartialSend(object state)
        {
            var context = (LongPollingTransportContext)state;

            if (!context.Transport.IsAlive)
            {
                return TaskAsyncHelper.Empty;
            }

            if (context.Transport.IsJsonp)
            {
                context.Transport.OutputWriter.Write(context.Transport.JsonpCallback);
                context.Transport.OutputWriter.Write("(");
            }

            context.Transport.JsonSerializer.Serialize(context.State, context.Transport.OutputWriter);

            if (context.Transport.IsJsonp)
            {
                context.Transport.OutputWriter.Write(");");
            }

            context.Transport.OutputWriter.Flush();

            return context.Transport.Context.Response.Flush();
        }

        private static Task PerformCompleteSend(object state)
        {
            var context = (LongPollingTransportContext)state;

            if (!context.Transport.IsAlive)
            {
                return TaskAsyncHelper.Empty;
            }

            context.Transport.Context.Response.ContentType = context.Transport.IsJsonp ?
                JsonUtility.JavaScriptMimeType :
                JsonUtility.JsonMimeType;

            return PerformPartialSend(state);
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
    }
}
