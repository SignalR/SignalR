using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SignalR.Transports
{
    public class ForeverFrameTransport : ForeverTransport
    {
        private const string _initTemplate = "<!DOCTYPE html><html><head>" +
                                             "<title>SignalR Forever Frame Transport Stream</title></head>\r\n" +
                                             "<body>\r\n" +
                                             "<script>\r\n" +
                                             //"    debugger;\r\n"+
                                             "    var $ = window.parent.jQuery,\r\n" +
                                             "        ff = $.signalR.transports.foreverFrame,\r\n" +
                                             "        c = ff.getConnection('{0}'),\r\n" +
                                             "        r = ff.receive;\r\n" +
                                             "    ff.started(c);" +
                                             "</script>";

        private const string _sendTemplate = "<script>r(c, {0});</script>\r\n";

        private const string _debugTemplate = "<div>{0}</div>\r\n";

        private readonly bool _isDebug;

        // TODO: Add heartbeat support for disconnect
        public ForeverFrameTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : base(context, jsonSerializer)
        {
            _isDebug = context.IsDebuggingEnabled;
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
            Context.Response.Flush();
        }

        public override void Send(PersistentResponse response)
        {
            var payload = JsonSerializer.Stringify(response);
            OnSending(payload);

            var script = String.Format(_sendTemplate, payload);
            Context.Response.Write(script);

            if (_isDebug)
            {
                Context.Response.Write(String.Format(_debugTemplate, payload));
            }

            Context.Response.Flush();
        }
    }
}