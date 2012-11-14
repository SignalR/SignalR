// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Transports
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

        private HTMLTextWriter _htmlOutputWriter;

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
        }

        public override TextWriter OutputWriter
        {
            get
            {
                if (_htmlOutputWriter == null)
                {
                    _htmlOutputWriter = new HTMLTextWriter(new StreamWriter(Context.Response.AsStream(), Encoding.UTF8));
                    _htmlOutputWriter.NewLine = "\n";
                }

                return _htmlOutputWriter;
            }
        }

        public HTMLTextWriter HTMLOutputWriter
        {
            get
            {
                if (_htmlOutputWriter == null)
                {
                    _htmlOutputWriter = new HTMLTextWriter(new StreamWriter(Context.Response.AsStream(), Encoding.UTF8));
                    _htmlOutputWriter.NewLine = "\n";
                }

                return _htmlOutputWriter;
            }
        }

        public override Task KeepAlive()
        {
            return EnqueueOperation(() =>
            {
                HTMLOutputWriter.WriteRaw("<script>r(c, {});</script>");
                HTMLOutputWriter.WriteLine();
                HTMLOutputWriter.WriteLine();
                HTMLOutputWriter.Flush();

                return Context.Response.FlushAsync();
            });
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            return EnqueueOperation(() =>
            {
                HTMLOutputWriter.WriteRaw("<script>r(c, ");
                JsonSerializer.Serialize(response, HTMLOutputWriter);
                HTMLOutputWriter.WriteRaw(");</script>\r\n");
                HTMLOutputWriter.Flush();

                return Context.Response.FlushAsync();
            });
        }

        protected override Task InitializeResponse(ITransportConnection connection)
        {
            return base.InitializeResponse(connection)
                .Then(initScript =>
                {
                    Context.Response.ContentType = "text/html";

                    return EnqueueOperation(() =>
                    {
                        HTMLOutputWriter.WriteRaw(initScript);
                        HTMLOutputWriter.Flush();

                        return Context.Response.FlushAsync();
                    });
                },
                _initPrefix + Context.Request.QueryString["frameId"] + _initSuffix);
        }

        public class HTMLTextWriter : TextWriter
        {
            private readonly TextWriter _writer;

            public HTMLTextWriter(TextWriter writer)
            {
                _writer = writer;
            }

            public override Encoding Encoding
            {
                get { return _writer.Encoding; }
            }

            public void WriteRaw(string value)
            {
                _writer.Write(value);
                Debug.Write(value);
            }

            public override void Write(string value)
            {
                Debug.Write(EscapeAnyInlineScriptTags(value));
                _writer.Write(EscapeAnyInlineScriptTags(value));
            }

            public override void WriteLine(string value)
            {
                Debug.WriteLine(EscapeAnyInlineScriptTags(value));
                _writer.WriteLine(EscapeAnyInlineScriptTags(value));
            }

            public override void WriteLine()
            {
                Debug.WriteLine("");
                _writer.WriteLine();
            }

            private static string EscapeAnyInlineScriptTags(string input)
            {
                return input.Replace("</script>", "</\"+\"script>");
            }
        }
    }
}
