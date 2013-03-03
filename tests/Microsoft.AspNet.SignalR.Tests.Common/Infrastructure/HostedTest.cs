using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public abstract class HostedTest : IDisposable
    {
        private IDisposable _systemNetLogging;
        
        protected ITestHost CreateHost(HostType hostType)
        {
            return CreateHost(hostType, TransportType.Auto);
        }

        protected ITestHost CreateHost(HostType hostType, TransportType transportType)
        {
            string testName = GetTestName() + "." + hostType + "." + transportType;
            ITestHost host = null;

            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            string clientTracePath = Path.Combine(logBasePath, testName + ".client.trace.log");
            string clientNetworkPath = Path.Combine(logBasePath, testName + ".client.network.log");

            if (hostType != HostType.Memory)
            {
                // Enable system new logging to this path
                _systemNetLogging = SystemNetLogging.Enable(clientNetworkPath);
            }

            switch (hostType)
            {
                case HostType.IISExpress:
                    host = new IISExpressTestHost(testName);
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.Memory:
                    var mh = new MemoryHost();
                    host = new MemoryTestHost(mh, Path.Combine(logBasePath, testName));
                    host.TransportFactory = () => CreateTransport(transportType, mh);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.Owin:
                    host = new OwinTestHost();
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    break;
                default:
                    break;
            }

            var writer = new StreamWriter(clientTracePath);
            writer.AutoFlush = true;
            host.ClientTraceOutput = writer;

            return host;
        }

        protected HubConnection CreateHubConnection(ITestHost host)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            var connection = new HubConnection(host.Url, query);
            connection.Trace = host.ClientTraceOutput ?? connection.Trace;
            return connection;
        }

        protected Client.Connection CreateConnection(ITestHost host, string path)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            var connection = new Client.Connection(host.Url + path, query);
            connection.Trace = host.ClientTraceOutput ?? connection.Trace;
            return connection;
        }

        protected string GetTestName()
        {
            var stackTrace = new StackTrace();
            return (from f in stackTrace.GetFrames()
                    select f.GetMethod() into m
                    let anyFactsAttributes = m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                    let anyTheories = m.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0
                    where anyFactsAttributes || anyTheories
                    select GetName(m)).First();
        }

        private string GetName(MethodBase m)
        {
            return m.DeclaringType.FullName.Substring(m.DeclaringType.Namespace.Length).TrimStart('.', '+') + "." + m.Name;
        }

        protected IClientTransport CreateTransport(TransportType transportType)
        {
            return CreateTransport(transportType, new DefaultHttpClient());
        }

        protected IClientTransport CreateTransport(TransportType transportType, IHttpClient client)
        {
            switch (transportType)
            {
                case TransportType.Websockets:
                    return new WebSocketTransport(client);
                case TransportType.ServerSentEvents:
                    return new ServerSentEventsTransport(client)
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(10)
                    };
                case TransportType.ForeverFrame:
                    break;
                case TransportType.LongPolling:
                    return new LongPollingTransport(client);
                default:
                    return new AutoTransport(client);
            }

            throw new NotSupportedException("Transport not supported");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);

            if (_systemNetLogging != null)
            {
                _systemNetLogging.Dispose();
            }
        }

        private static class SystemNetLogging
        {
            private static readonly Lazy<Logging> _logging = new Lazy<Logging>(() => new Logging());

            public static IDisposable Enable(string path)
            {
                var listener = new TextWriterTraceListener(path);
                _logging.Value.Sources.ForEach(s => s.Listeners.Add(listener));

                return new DisposableAction(() =>
                {
                    listener.Flush();
                    _logging.Value.Sources.ForEach(s => s.Listeners.Remove(listener));
                    listener.Close();
                });
            }

            private class Logging
            {
                private readonly Type _loggingType;

                public List<TraceSource> Sources = new List<TraceSource>();

                public Logging()
                {
                    _loggingType = Type.GetType("System.Net.Logging, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

                    if (_loggingType == null)
                    {
                        return;
                    }

                    var webProperty = _loggingType.GetProperty("Web", BindingFlags.NonPublic | BindingFlags.Static);
                    if (webProperty != null)
                    {
                        Sources.Add((TraceSource)webProperty.GetValue(null));
                    }

                    var socketsProperty = _loggingType.GetProperty("Sockets", BindingFlags.NonPublic | BindingFlags.Static);
                    if (socketsProperty != null)
                    {
                        Sources.Add((TraceSource)socketsProperty.GetValue(null));
                    }

                    var webSocketsProperty = _loggingType.GetProperty("WebSockets", BindingFlags.NonPublic | BindingFlags.Static);
                    if (webSocketsProperty != null)
                    {
                        Sources.Add((TraceSource)webSocketsProperty.GetValue(null));
                    }
                }
            }
        }
    }
}
