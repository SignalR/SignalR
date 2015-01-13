// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public static class HostedTestFactory
    {
        public static ITestHost CreateHost(string hostTypeName, string transportName, string testName, string url = null)
        {
            HostType hostType;
            if (!Enum.TryParse<HostType>(hostTypeName, true, out hostType))
            {
                // default it to Memory Host 
                hostType = HostType.Memory;
            }

            TransportType transportType;
            if (!Enum.TryParse<TransportType>(transportName, true, out transportType))
            {
                // default it to Long Polling for transport
                transportType = TransportType.LongPolling;
            }

            return CreateHost(hostType, transportType, testName, url);
        }

        public static ITestHost CreateHost(HostType hostType, TransportType transportType, string testName, string url = null)
        {
            ITestHost host =  null;

            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            TraceListener traceListener = EnableTracing(testName, logBasePath);

            switch (hostType)
            {
                case HostType.IISExpress:
                    host = new IISExpressTestHost(testName);
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.External:
                    host = new ExternalTestHost(url);
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.Memory:
                default: 
                    var mh = new MemoryHost();
                    host = new MemoryTestHost(mh, Path.Combine(logBasePath, testName));
                    host.TransportFactory = () => CreateTransport(transportType, mh);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.HttpListener:
                    host = new OwinTestHost(Path.Combine(logBasePath, testName));
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    Trace.TraceInformation("HttpListener url: {0}", host.Url);
                    break;
            }

            var writer = CreateClientTraceWriter(testName);
            host.ClientTraceOutput = writer;

            if (hostType != HostType.Memory && hostType != HostType.External)
            {
                string clientNetworkPath = Path.Combine(logBasePath, testName + ".client.network.log");
                host.Disposables.Add(SystemNetLogging.Enable(clientNetworkPath));

                string httpSysTracePath = Path.Combine(logBasePath, testName + ".httpSys");
                IDisposable httpSysTracing = StartHttpSysTracing(httpSysTracePath);

                // If tracing is enabled then turn it off on host dispose
                if (httpSysTracing != null)
                {
                    host.Disposables.Add(httpSysTracing);
                }
            }

            host.Disposables.Add(new DisposableAction(() =>
            {
                traceListener.Close();
                Trace.Listeners.Remove(traceListener);
            }));

            EventHandler<UnobservedTaskExceptionEventArgs> handler = (sender, args) =>
            {
                Trace.TraceError("Unobserved task exception: " + args.Exception.GetBaseException());

                args.SetObserved();
            };

            TaskScheduler.UnobservedTaskException += handler;
            host.Disposables.Add(new DisposableAction(() =>
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }));

            return host;
        }

        public static IClientTransport CreateTransport(TransportType transportType)
        {
            return CreateTransport(transportType, new DefaultHttpClient());
        }

        public static IClientTransport CreateTransport(TransportType transportType, IHttpClient client)
        {
            switch (transportType)
            {
                case TransportType.Websockets:
                    return new WebSocketTransport(client);
                case TransportType.ServerSentEvents:
                    return new ServerSentEventsTransport(client);
                case TransportType.ForeverFrame:
                    break;
                case TransportType.LongPolling:
                    return new LongPollingTransport(client);
                default:
                    return new AutoTransport(client);
            }

            throw new NotSupportedException("Transport not supported");
        }

        public static TextWriterTraceListener EnableTracing(string testName, string logBasePath)
        {
            string testTracePath = Path.Combine(logBasePath, testName + ".test.trace.log");
            var traceListener = new TextWriterTraceListener(testTracePath);
            Trace.Listeners.Add(traceListener);
            Trace.AutoFlush = true;
            return traceListener;
        }

        public static StreamWriter CreateClientTraceWriter(string testName)
        {
            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            string clientTracePath = Path.Combine(logBasePath, testName + ".client.trace.log");
            var writer = new StreamWriter(clientTracePath);
            writer.AutoFlush = true;
            return writer;
        }

        private static IDisposable StartHttpSysTracing(string path)
        {
            var httpSysLoggingEnabledValue = ConfigurationManager.AppSettings["httpSysLoggingEnabled"];
            bool httpSysLoggingEnabled;

            if (!Boolean.TryParse(httpSysLoggingEnabledValue, out httpSysLoggingEnabled) ||
                !httpSysLoggingEnabled)
            {
                return null;
            }

            var etw = new HttpSysEtwWrapper(path);
            if (etw.StartLogging())
            {
                return etw;
            }

            return null;
        }
    }
}
