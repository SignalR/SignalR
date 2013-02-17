// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        private readonly Func<object, Task> _send;
        private readonly Func<Task> _writeInit;
        private readonly Func<Task> _initializeResponse;

        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
            _send = PerfomSend;
            _writeInit = WriteInit;
            _initializeResponse = InitializeResponse;
        }

        public override Task KeepAlive()
        {
            if (InitializeTcs == null || !InitializeTcs.Task.IsCompleted)
            {
                return TaskAsyncHelper.Empty;
            }

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformKeepAlive(state), this);
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            return EnqueueOperation(_send, response);
        }

        protected internal override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                       .Then(_initializeResponse);
        }

        private Task InitializeResponse()
        {
            return EnqueueOperation(_writeInit);
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (ServerSentEventsTransport)state;

            transport.OutputWriter.Write("data: {}");
            transport.OutputWriter.WriteLine();
            transport.OutputWriter.WriteLine();
            transport.OutputWriter.Flush();

            return transport.Context.Response.Flush();
        }

        private Task PerfomSend(object state)
        {
            OutputWriter.Write("data: ");
            JsonSerializer.Serialize(state, OutputWriter);
            OutputWriter.WriteLine();
            OutputWriter.WriteLine();
            OutputWriter.Flush();

            return Context.Response.Flush();
        }

        private Task WriteInit()
        {
            Context.Response.ContentType = "text/event-stream";

            // "data: initialized\n\n"
            OutputWriter.Write("data: initialized");
            OutputWriter.WriteLine();
            OutputWriter.WriteLine();
            OutputWriter.Flush();

            return Context.Response.Flush();
        }
    }
}
