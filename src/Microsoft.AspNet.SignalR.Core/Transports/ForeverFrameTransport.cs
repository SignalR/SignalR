// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable fields are disposed from a different method")]
    public class ForeverFrameTransport : ForeverTransport
    {
        private const string _initFormat = @"<html>
                                             <head>
                                             <script>
                                                 var frameId = {0},
                                                     ff = null,
                                                     c = null,
                                                     m = null,
                                                     r = function(data) { m('receive', data); }; 

                                                 // try to call methods directly, fall back to postMessage
                                                 try {
                                                    // this will throw an error if domains do not match
                                                    ff = window.parent.jQuery.signalR.transports.foreverFrame;
                                                    c = ff.getConnection(frameId);

                                                    m = function(method, data) {
                                                        ff.receiveMessage(c, method, data);
                                                        document.body.innerHTML = '';
                                                    };
                                                 } catch (err) {
                                                    m = function(method, data) {
                                                        // IE 8,9 only support string messages
                                                        var dataString = JSON.stringify({
                                                            foreverFrameId : frameId,
                                                            method : method,
                                                            data : data });

                                                        window.parent.postMessage(dataString, '*');
                                                        document.body.innerHTML = '';
                                                    };
                                                 }

                                                 // the page should never fully load, if it does, we need to reconnect
                                                 window.onload = function(){ m('reconnect'); };

                                                 m('started');
                                             </script></head>
                                             <body>";

        private static readonly Regex shrink_ray = new Regex(@"(^|\n|\r)+\s+|\s+(&|\n\r)+|//.*");

        private static readonly string smaller_init = GetSmallerInit();

        private static string GetSmallerInit()
        {
            return shrink_ray.Replace(_initFormat, "");
        }

        private HTMLTextWriter _htmlOutputWriter;

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : base(context, resolver)
        {
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

        private HTMLTextWriter HTMLOutputWriter
        {
            get
            {
                if (_htmlOutputWriter == null)
                {
                    _htmlOutputWriter = new HTMLTextWriter(Context.Response);
                    _htmlOutputWriter.NewLine = "\n";
                }

                return _htmlOutputWriter;
            }
        }

        public override Task KeepAlive()
        {
            if (InitializeTcs == null || !InitializeTcs.Task.IsCompleted)
            {
                return TaskAsyncHelper.Empty;
            }

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformKeepAlive(state), this);
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            var context = new ForeverFrameTransportContext(this, response);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(s => PerformSend(s), context);
        }

        protected internal override Task InitializeResponse(ITransportConnection connection)
        {
            uint frameId;
            string rawFrameId = Context.Request.QueryString["frameId"];
            if (String.IsNullOrWhiteSpace(rawFrameId) || !UInt32.TryParse(rawFrameId, NumberStyles.None, CultureInfo.InvariantCulture, out frameId))
            {
                // Invalid frameId passed in
                throw new InvalidOperationException(Resources.Error_InvalidForeverFrameId);
            }

            // string.Format doesn't like all of the braces in Javascript, this works for our purposes
            string initScript = smaller_init.Replace("{0}", frameId.ToString(CultureInfo.InvariantCulture));

            var context = new ForeverFrameTransportContext(this, initScript);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return base.InitializeResponse(connection).Then(s => Initialize(s), context);
        }

        private static Task Initialize(object state)
        {
            var context = (ForeverFrameTransportContext)state;

            var initContext = new ForeverFrameTransportContext(context.Transport, context.State);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return context.Transport.EnqueueOperation(s => WriteInit(s), initContext);
        }

        private static Task WriteInit(object state)
        {
            var context = (ForeverFrameTransportContext)state;

            context.Transport.Context.Response.ContentType = "text/html; charset=UTF-8";

            context.Transport.HTMLOutputWriter.WriteRaw((string)context.State);
            context.Transport.HTMLOutputWriter.Flush();

            return context.Transport.Context.Response.Flush();
        }

        private static Task PerformSend(object state)
        {
            var context = (ForeverFrameTransportContext)state;

            context.Transport.HTMLOutputWriter.WriteRaw("<script>r(");
            context.Transport.JsonSerializer.Serialize(context.State, context.Transport.HTMLOutputWriter);
            context.Transport.HTMLOutputWriter.WriteRaw(");</script>\r\n");
            context.Transport.HTMLOutputWriter.Flush();

            return context.Transport.Context.Response.Flush();
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (ForeverFrameTransport)state;

            transport.HTMLOutputWriter.WriteRaw("<script>r({});</script>");
            transport.HTMLOutputWriter.WriteLine();
            transport.HTMLOutputWriter.WriteLine();
            transport.HTMLOutputWriter.Flush();

            return transport.Context.Response.Flush();
        }

        private class ForeverFrameTransportContext
        {
            public ForeverFrameTransport Transport;
            public object State;

            public ForeverFrameTransportContext(ForeverFrameTransport transport, object state)
            {
                Transport = transport;
                State = state;
            }
        }

        private class HTMLTextWriter : BufferTextWriter
        {
            public HTMLTextWriter(IResponse response)
                : base(response)
            {
            }

            public void WriteRaw(string value)
            {
                base.Write(value);
            }

            public override void Write(string value)
            {
                base.Write(JavascriptEncode(value));
            }

            public override void WriteLine(string value)
            {
                base.WriteLine(JavascriptEncode(value));
            }

            private static string JavascriptEncode(string input)
            {
                return input.Replace("<", "\\u003c").Replace(">", "\\u003e");
            }
        }
    }
}
