using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SignalR.Transports {
    public class ServerSentEventsTransport : ForeverTransport {
        public ServerSentEventsTransport(HttpContextBase context, IJsonStringifier jsonStringifier)
            : base(context, jsonStringifier) {

        }

        protected override void InitializeResponse(IConnection connection) {
            base.InitializeResponse(connection);
            Context.Response.ContentType = "text/event-stream";
            Context.Response.CacheControl = "no-cache";
            Context.Response.AddHeader("Connection", "keep-alive");
            Context.Response.Write("data:initialized\n\n");
            Context.Response.Flush();
        }

        protected override void Send(PersistentResponse response) {
            Context.Response.Write("id:" + response.MessageId + "\n");
            Context.Response.Flush();
            
            Context.Response.Write("data:" +
                String.Join("\ndata:", JsonStringifier.Stringify(response).Split(new [] { "\r\n" }, StringSplitOptions.None))
                + "\n\n");
            Context.Response.Flush();

            Context.Response.Write("\n\n");
            Context.Response.Flush();
        }
    }
}