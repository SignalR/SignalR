using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SignalR.Transports
{
    public class ForeverFrameTransport : ForeverTransport
    {
        private const string _initPrefix = "<!DOCTYPE html>" +
                                           "<html>" +
                                           "<head>" +
                                           "<title>SignalR Forever Frame Transport Stream</title>\r\n" +
                                           "<script>\r\n" + //"    debugger;\r\n"+
                                           "    var $ = window.parent.jQuery,\r\n" +
                                           "        ff = $ ? $.signalR.transports.foreverFrame : null,\r\n" +
                                           "        c =  ff ? ff.getConnection('";

        private const string _initSuffix = "') : null,\r\n" +
                                            "        r = ff ? ff.receive : function() {};\r\n" +
                                            "    ff ? ff.started(c) : '';" +
                                            "</script></head>" + 
                                            "<body>\r\n";

        private readonly bool _isDebug;

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
            _isDebug = context.IsDebuggingEnabled();
        }

        public override void KeepAlive()
        {
            var script = "<script>r(c, {});</script>\r\n";
            WriteAsync(script).Catch();
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);

            OnSending(data);

            OnSendingResponse(response);

            data = EscapeAnyInlineScriptTags(data);

            var script = "<script>r(c, " + data + ");</script>\r\n";
            if (_isDebug)
            {
                script += "<div>" + data + "</div>\r\n";
            }

            return WriteAsync(script);
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                .Then(initScript =>
                {
                    Context.Response.ContentType = "text/html";
                    WriteAsync(initScript);
                },
                _initPrefix + Context.Request.QueryString["frameId"] + _initSuffix);
        }

        private static string EscapeAnyInlineScriptTags(string input)
        {
            return input.Replace("</script>", "</\"+\"script>");
        }
    }
}