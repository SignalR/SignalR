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
                                             "        ff = $ ? $.signalR.transports.foreverFrame : null,\r\n" +
                                             "        c =  ff ? ff.getConnection('{0}') : null,\r\n" +
                                             "        r = ff ? ff.receive : function() {{}};\r\n" +
                                             "    ff ? ff.started(c) : '';" +
                                             "</script>";

        private const string _sendTemplate = "<script>r(c, {0});</script>\r\n";

        private const string _debugTemplate = "<div>{0}</div>\r\n";

        private readonly bool _isDebug;

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

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);
            OnSending(data);

            var script = String.Format(_sendTemplate, data);
            if (_isDebug)
            {
                script += (String.Format(_debugTemplate, data));
            }

            if (Context.Response.IsClientConnected)
            {
                return Context.Response.WriteAsync(script);
            }
            return TaskAsyncHelper.Empty;
        }

        protected override Task InitializeResponse(IConnection connection)
        {
            return base.InitializeResponse(connection)
                .Then(initScript => Context.Response.WriteAsync(initScript), String.Format(_initTemplate, Context.Request.QueryString["frameId"]))
                .FastUnwrap();
        }
    }
}