// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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

        HTMLTextWriter _htmlOutputWriter;

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
        }

        private HTMLTextWriter HTMLOutputWriter
        {
            get
            {
                if (_htmlOutputWriter == null)
                {
                    _htmlOutputWriter = new HTMLTextWriter(Context.Response.AsStream(), Encoding.UTF8);
                    _htmlOutputWriter.NewLine = "\n";
                }

                return _htmlOutputWriter;
            }
        }

        /// <summary>
        /// Pointed to the HTMLOutputWriter to wrap output stream with an HTML friendly one
        /// </summary>
        public override TextWriter OutputWriter
        {
            get
            {
                return HTMLOutputWriter;
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

        private class HTMLTextWriter : StreamWriter
        {
            public HTMLTextWriter(Stream stream, Encoding encoding)
                : base(stream, encoding)
            {
            }

            public void WriteRaw(string value)
            {
                base.Write(value);
            }

            public override void Write(string value)
            {
                base.Write(EscapeAnyInlineScriptTags(value));
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(EscapeAnyInlineScriptTags(value));
            }

            private static string EscapeAnyInlineScriptTags(string input)
            {
                return input.Replace("</script>", "</\"+\"script>");
            }
        }
    }
}
