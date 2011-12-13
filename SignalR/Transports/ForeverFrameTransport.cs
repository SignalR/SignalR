using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using SignalR.Infrastructure;

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

        protected override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Path.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override Task InitializeResponse(IConnection connection)
        {
            return base.InitializeResponse(connection)
                .Then(_ => Context.Response.WriteAsync(String.Format(_initTemplate, Context.Request.QueryString["frameId"])))
                .FastUnwrap();
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);
            OnSending(data);

            var payload = String.Format(_sendTemplate, data);
            if (_isDebug)
            {
                payload += (String.Format(_debugTemplate, data));
            }

            if (Context.Response.IsClientConnected)
            {
                return Context.Response.WriteAsync(payload);
            }
            return TaskAsyncHelper.Empty;
        }
    }
}