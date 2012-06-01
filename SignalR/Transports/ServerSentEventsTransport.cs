using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Hosting;

namespace SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        public ServerSentEventsTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {

        }

        public override void KeepAlive()
        {
            Debug.WriteLine("Sending empty keep alive packet to client");
            Context.Response.WriteAsync("data: {}\n\n").Catch();
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);
            OnSending(data);

            return Context.Response.WriteAsync("id: " + response.MessageId + "\n" + "data: " + data + "\n\n");
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                       .Then(() =>
                       {
                           Context.Response.ContentType = "text/event-stream";
                           return Context.Response.WriteAsync("data: initialized\n\n");
                       });
        }
    }
}