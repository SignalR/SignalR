using System;
using System.Web;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        public ServerSentEventsTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : base(context, jsonSerializer)
        {

        }

        protected override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Headers["Last-Event-ID"] == null;
            }
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);
            OnSending(data);

            if (Context.Response.IsClientConnected)
            {
                return Context.Response.WriteAsync("id: " + response.MessageId + "\n" + "data: " + data + "\n\n");
            }
            return TaskAsyncHelper.Empty;
        }

        protected override Task InitializeResponse(IConnection connection)
        {
            long lastMessageId;
            if (long.TryParse(Context.Request.Headers["Last-Event-ID"], out lastMessageId))
            {
                LastMessageId = lastMessageId;
            }

            return base.InitializeResponse(connection)
                .Then(() =>
                {
                    Context.Response.ContentType = "text/event-stream";
                    return Context.Response.WriteAsync("data: initialized\n\n");
                }).FastUnwrap();
        }
    }
}