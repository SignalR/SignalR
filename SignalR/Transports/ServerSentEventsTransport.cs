using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
        }

        public override Task KeepAlive()
        {
            OutputWriter.Write("data: {}");
            OutputWriter.WriteLine();
            OutputWriter.WriteLine();
            OutputWriter.Flush();

            return Context.Response.FlushAsync();
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            OutputWriter.Write("id: ");
            OutputWriter.Write(response.MessageId);
            OutputWriter.WriteLine();
            OutputWriter.Write("data: ");
            JsonSerializer.Serialize(response, OutputWriter);
            OutputWriter.WriteLine();
            OutputWriter.WriteLine();
            OutputWriter.Flush();

            return Context.Response.FlushAsync().Catch(IncrementErrorCounters);
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                       .Then(() =>
                       {
                           Context.Response.ContentType = "text/event-stream";

                           // "data: initialized\n\n"
                           OutputWriter.Write("data: initialized");
                           OutputWriter.WriteLine();
                           OutputWriter.WriteLine();
                           OutputWriter.Flush();

                           return Context.Response.FlushAsync();
                       });
        }
    }
}