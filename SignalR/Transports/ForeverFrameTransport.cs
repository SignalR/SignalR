using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SignalR.Transports
{
    public class ForeverFrameTransport : ForeverTransport
    {
        // TODO: Add support for echoing out data if no parent window found
        private const string _initTemplate = "<!DOCTYPE html><html><head><title>SignalR Forever Frame Transport Stream</title></head>\r\n" +
                                             "<body>\r\n" +
                                             "<script>\r\n" +
                                             "    debugger;\r\n"+
                                             "    var $ = window.parent.jQuery,\r\n" +
                                             "        ff = $.signalR.transports.foreverFrame,\r\n" +
                                             "        c = ff.getConnection('{0}'),\r\n" +
                                             "        r = ff.receive;\r\n" +
                                             "</script>\r\n";
        private const string _sendTemplate = "<script>r(c, {0});</script>\r\n";

        public ForeverFrameTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : base(context, jsonSerializer)
        {
            
        }

        protected string ClientID
        {
            get
            {
                return Context.Request.QueryString["clientID"];
            }
        }

        protected override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Path.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override void InitializeResponse(IConnection connection)
        {
            base.InitializeResponse(connection);
            Context.Response.Write(String.Format(_initTemplate, Context.Request.QueryString["frameId"]));
        }

        public override void Send(object value)
        {
            var payload = JsonSerializer.Stringify(value);
            OnSending(payload);

            var script = String.Format(_sendTemplate, payload);
            Context.Response.Write(script);
        }
    }
}