using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
            WriteAsync("data: {}\n\n").Catch();
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);
            
            var data = JsonSerializer.Stringify(response);

            OnSending(data);

            return WriteAsync("id: " + response.MessageId + "\n" + "data: " + data + "\n\n");
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                       .Then(() =>
                       {
                           Context.Response.ContentType = "text/event-stream";
                           return WriteAsync("data: initialized\n\n");
                       });
        }
    }
}