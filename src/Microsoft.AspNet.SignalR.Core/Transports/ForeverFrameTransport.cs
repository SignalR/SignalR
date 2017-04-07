﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable fields are disposed from a different method")]
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

        private readonly IPerformanceCounterManager _counters;

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver)
            : this(context, resolver, resolver.Resolve<IPerformanceCounterManager>())
        {
        }

        public ForeverFrameTransport(HostContext context, IDependencyResolver resolver, IPerformanceCounterManager performanceCounterManager)
            : base(context, resolver)
        {
            _counters = performanceCounterManager;
        }

        public override void IncrementConnectionsCount()
        {
            _counters.ConnectionsCurrentForeverFrame.Increment();
        }

        public override void DecrementConnectionsCount()
        {
            _counters.ConnectionsCurrentForeverFrame.Decrement();
        }

        public override Task KeepAlive()
        {
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

            string initScript = _initPrefix +
                                frameId.ToString(CultureInfo.InvariantCulture) +
                                _initSuffix;

            var context = new ForeverFrameTransportContext(this, initScript);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return base.InitializeResponse(connection).Then(s => Initialize(s), context);
        }

        internal override MemoryPoolTextWriter CreateMemoryPoolWriter(IMemoryPool memoryPool)
        {
            return new HTMLTextWriter(memoryPool);
        }

        private static Task Initialize(object state)
        {
            var context = (ForeverFrameTransportContext)state;

            var initContext = new ForeverFrameTransportContext(context.Transport, context.State);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return WriteInit(initContext);
        }

        private static Task WriteInit(ForeverFrameTransportContext context)
        {
            context.Transport.Context.Response.ContentType = "text/html; charset=UTF-8";

            using (var htmlOutputWriter = new HTMLTextWriter(context.Transport.Pool))
            {
                htmlOutputWriter.NewLine = "\n";

                htmlOutputWriter.WriteRaw((string)context.State);
                htmlOutputWriter.Flush();

                context.Transport.Context.Response.Write(htmlOutputWriter.Buffer);
            }

            return context.Transport.Context.Response.Flush();
        }

        private static Task PerformSend(object state)
        {
            var context = (ForeverFrameTransportContext)state;

            using (var htmlOutputWriter = new HTMLTextWriter(context.Transport.Pool))
            {
                htmlOutputWriter.NewLine = "\n";
                htmlOutputWriter.WriteRaw("<script>r(c, ");
                context.Transport.JsonSerializer.Serialize(context.State, htmlOutputWriter);
                htmlOutputWriter.WriteRaw(");</script>\r\n");
                htmlOutputWriter.Flush();

                context.Transport.Context.Response.Write(htmlOutputWriter.Buffer);
            }

            return context.Transport.Context.Response.Flush();
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (ForeverFrameTransport)state;

            using (var htmlOutputWriter = new HTMLTextWriter(transport.Pool))
            {
                htmlOutputWriter.NewLine = "\n";
                htmlOutputWriter.WriteRaw("<script>r(c, {});</script>");
                htmlOutputWriter.WriteLine();
                htmlOutputWriter.WriteLine();
                htmlOutputWriter.Flush();

                transport.Context.Response.Write(htmlOutputWriter.Buffer);
            }

            return transport.Context.Response.Flush();
        }

        private struct ForeverFrameTransportContext
        {
            public readonly ForeverFrameTransport Transport;
            public readonly object State;

            public ForeverFrameTransportContext(ForeverFrameTransport transport, object state)
            {
                Transport = transport;
                State = state;
            }
        }

        private class HTMLTextWriter : MemoryPoolTextWriter
        {
            public HTMLTextWriter(IMemoryPool pool) :
                base(pool)
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
