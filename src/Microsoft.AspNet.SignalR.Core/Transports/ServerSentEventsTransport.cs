// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        private readonly IPerformanceCounterManager _counters;
        private static ArraySegment<byte> _keepAlive = new ArraySegment<byte>(Encoding.UTF8.GetBytes("data: {}\n\n"));
        private static ArraySegment<byte> _dataInitialized = new ArraySegment<byte>(Encoding.UTF8.GetBytes("data: initialized\n\n"));

        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver)
            : this(context, resolver, resolver.Resolve<IPerformanceCounterManager>())
        {
        }

        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver, IPerformanceCounterManager performanceCounterManager)
            : base(context, resolver)
        {
            _counters = performanceCounterManager;
        }

        public override Task KeepAlive()
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformKeepAlive(state), this);
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            var context = new SendContext(this, response);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformSend(state), context);
        }

        public override void IncrementConnectionsCount()
        {
            _counters.ConnectionsCurrentServerSentEvents.Increment();
        }

        public override void DecrementConnectionsCount()
        {
            _counters.ConnectionsCurrentServerSentEvents.Decrement();
        }

        protected internal override Task InitializeResponse(ITransportConnection connection)
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return base.InitializeResponse(connection)
                       .Then(s => WriteInit(s), this);
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (ServerSentEventsTransport)state;

            transport.Context.Response.Write(_keepAlive);

            return transport.Context.Response.Flush();
        }

        private static Task PerformSend(object state)
        {
            var context = (SendContext)state;

            using (var writer = new BinaryTextWriter(context.Transport.Context.Response))
            {
                writer.Write("data: ");
                context.Transport.JsonSerializer.Serialize(context.State, writer);
                writer.WriteLine();
                writer.WriteLine();
                writer.Flush();
            }

            return context.Transport.Context.Response.Flush();
        }

        private static Task WriteInit(ServerSentEventsTransport transport)
        {
            transport.Context.Response.ContentType = "text/event-stream";

            transport.Context.Response.Write(_dataInitialized);
            
            return transport.Context.Response.Flush();
        }

        private struct SendContext
        {
            public readonly ServerSentEventsTransport Transport;
            public readonly object State;

            public SendContext(ServerSentEventsTransport transport, object state)
            {
                Transport = transport;
                State = state;
            }
        }
    }
}
