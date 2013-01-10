// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
        }

        public override Task KeepAlive()
        {
            if (InitializeTcs == null || !InitializeTcs.Task.IsCompleted)
            {
                return TaskAsyncHelper.Empty;
            }

            return EnqueueOperation(() =>
            {
                OutputWriter.Write("data: {}");
                OutputWriter.WriteLine();
                OutputWriter.WriteLine();
                OutputWriter.Flush();

                return Context.Response.Flush();
            });
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            return EnqueueOperation(() =>
            {
                OutputWriter.Write("data: ");
                JsonSerializer.Serialize(response, OutputWriter);
                OutputWriter.WriteLine();
                OutputWriter.WriteLine();
                OutputWriter.Flush();

                return Context.Response.Flush();
            });
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                       .Then(() =>
                       {
                           Context.Response.ContentType = "text/event-stream";

                           return EnqueueOperation(() =>
                           {
                               // "data: initialized\n\n"
                               OutputWriter.Write("data: initialized");
                               OutputWriter.WriteLine();
                               OutputWriter.WriteLine();
                               OutputWriter.Flush();

                               return Context.Response.Flush();
                           });
                       });
        }
    }
}
