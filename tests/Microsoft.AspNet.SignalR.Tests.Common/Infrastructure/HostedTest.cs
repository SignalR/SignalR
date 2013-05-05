using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.FunctionalTests.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public abstract class HostedTest : IDisposable
    {
        private static long _id;

        protected ITestHost CreateHost(HostType hostType)
        {
            return CreateHost(hostType, TransportType.Auto);
        }

        protected ITestHost CreateHost(HostType hostType, TransportType transportType)
        {
            string testName = GetTestName() + "." + hostType + "." + transportType + "." + Interlocked.Increment(ref _id);
            ITestHost host = null;

            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");

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

            var writer = CreateClientTraceWriter(testName);
            host.ClientTraceOutput = writer;

            if (hostType != HostType.Memory)
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

            TraceListener traceListener = EnableTracing(testName, logBasePath);

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

        protected void UseMessageBus(MessageBusType type, IDependencyResolver resolver, ScaleoutConfiguration configuration = null, int streams = 1)
        {
            switch (type)
            {
                case MessageBusType.Default:
                    break;
                case MessageBusType.Fake:
                    var bus = new FakeScaleoutBus(resolver, streams);
                    resolver.Register(typeof(IMessageBus), () => bus);
                    break;
                case MessageBusType.SqlServer:
                    break;
                case MessageBusType.ServiceBus:
                    break;
                case MessageBusType.Redis:
                    break;
                default:
                    break;
            }
        }

        protected void EnableTracing()
        {
            string testName = GetTestName();
            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            EnableTracing(GetTestName(), logBasePath);
        }

        private TextWriterTraceListener EnableTracing(string testName, string logBasePath)
        {
            string testTracePath = Path.Combine(logBasePath, testName + ".test.trace.log");
            var traceListener = new TextWriterTraceListener(testTracePath);
            Trace.Listeners.Add(traceListener);
            Trace.AutoFlush = true;
            return traceListener;
        }

        private static StreamWriter CreateClientTraceWriter(string testName)
        {
            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            string clientTracePath = Path.Combine(logBasePath, testName + ".client.trace.log");
            var writer = new StreamWriter(clientTracePath);
            writer.AutoFlush = true;
            return writer;
        }

        private IDisposable StartHttpSysTracing(string path)
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

        protected HubConnection CreateHubConnection(string url)
        {
            string testName = GetTestName();
            var query = new Dictionary<string, string>();
            query["test"] = testName;
            var connection = new HubConnection(url, query);
            connection.TraceWriter = CreateClientTraceWriter(testName);
            return connection;
        }

        protected HubConnection CreateHubConnection(ITestHost host, string url = null, bool useDefaultUrl = true)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            SetHostData(host, query);
            var connection = new HubConnection(url ?? host.Url, query, useDefaultUrl);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
            return connection;
        }

        protected Client.Connection CreateConnection(ITestHost host, string path)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            SetHostData(host, query);
            var connection = new Client.Connection(host.Url + path, query);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
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

        protected void SetHostData(ITestHost host, Dictionary<string, string> query)
        {
            foreach (var item in host.ExtraData)
            {
                query[item.Key] = item.Value;
            }
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
        }

        private class FakeScaleoutBus : ScaleoutMessageBus
        {
            private int _streams;
            private ulong _id;

            public FakeScaleoutBus(IDependencyResolver resolver)
                : this(resolver, streams: 1)
            {
            }

            public FakeScaleoutBus(IDependencyResolver dr, int streams)
                : base(dr, new ScaleoutConfiguration())
            {
                _streams = streams;

                for (int i = 0; i < _streams; i++)
                {
                    Open(i);
                }
            }

            protected override int StreamCount
            {
                get
                {
                    return _streams;
                }
            }

            protected override Task Send(int streamIndex, IList<Message> messages)
            {
                var message = new ScaleoutMessage(messages);

                OnReceived(streamIndex, _id, message);

                _id++;

                return TaskAsyncHelper.Empty;
            }
        }
    }
}
