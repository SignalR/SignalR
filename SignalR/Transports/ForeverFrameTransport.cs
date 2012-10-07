using System.Diagnostics;
using System.IO;
using System.Text;
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

        public override Task KeepAlive()
        {
            OutputWriter.Write("<script>r(c, {});</script>");
            OutputWriter.WriteLine();
            OutputWriter.WriteLine();
            OutputWriter.Flush();

            return Context.Response.FlushAsync();
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            OutputWriter.Write("<script>r(c, ");
            JsonSerializer.Serialize(response, OutputWriter);
            OutputWriter.Write(");</script>\r\n");
            OutputWriter.Flush();

            return Context.Response.FlushAsync().Catch(IncrementErrorCounters);
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                .Then(initScript =>
                {
                    Context.Response.ContentType = "text/html";
                    OutputWriter.Write(initScript);
                    OutputWriter.Flush();

                    return Context.Response.FlushAsync();
                },
                _initPrefix + Context.Request.QueryString["frameId"] + _initSuffix);
        }

        private class TextWriterWrapper : TextWriter
        {
            private readonly TextWriter _writer;

            public TextWriterWrapper(TextWriter writer)
            {
                _writer = writer;
            }

            public override Encoding Encoding
            {
                get { return _writer.Encoding; }
            }

            public override void Write(string value)
            {
                _writer.Write(EscapeAnyInlineScriptTags(value));
            }

            public override void WriteLine(string value)
            {
                _writer.Write(EscapeAnyInlineScriptTags(value));
            }

            public override void WriteLine()
            {
                _writer.WriteLine();
            }

            private static string EscapeAnyInlineScriptTags(string input)
            {
                return input.Replace("</script>", "</\"+\"script>");
            }
        }
    }
}