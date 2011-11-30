using System;
using System.Web;

namespace SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        public ServerSentEventsTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : base(context, jsonSerializer)
        {

        }

        protected override void InitializeResponse(IConnection connection)
        {
            long lastMessageId;
            if (long.TryParse(Context.Request.Headers["Last-Event-ID"], out lastMessageId))
            {
                LastMessageId = lastMessageId;
            }

            base.InitializeResponse(connection);

            Context.Response.ContentType = "text/event-stream";

            Context.Response.Write("data: initialized\n\n");
            Context.Response.Flush();
        }

        protected override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Headers["Last-Event-ID"] == null;
            }
        }

        public override void Send(PersistentResponse response)
        {
            Context.Response.Write("id: " + response.MessageId + "\n");
            Context.Response.Write("data: " + JsonSerializer.Stringify(response) + "\n\n");
            if (Context.Response.IsClientConnected)
            {
                Context.Response.Flush();
            }
        }
    }
}