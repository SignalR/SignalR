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

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
        }

        public override Task KeepAlive()
        {
            return EnqueueOperation(() =>
            {
                OutputWriter.Write("<script>r(c, {});</script>");
                OutputWriter.WriteLine();
                OutputWriter.WriteLine();
                OutputWriter.Flush();

                return Context.Response.FlushAsync();
            });
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            return EnqueueOperation(() =>
            {
                OutputWriter.Write("<script>r(c, ");
                JsonSerializer.Serialize(response, OutputWriter);
                OutputWriter.Write(");</script>\r\n");
                OutputWriter.Flush();

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
                        OutputWriter.Write(initScript);
                        OutputWriter.Flush();

                        return Context.Response.FlushAsync();
                    });
                },
                _initPrefix + Context.Request.QueryString["frameId"] + _initSuffix);
        }
    }
}
