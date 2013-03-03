using System;
using System.Collections.Generic;
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

            string clientTracePath = Path.Combine(logBasePath, testName + ".client.trace.log");
            var writer = new StreamWriter(clientTracePath);
            writer.AutoFlush = true;
            host.ClientTraceOutput = writer;

            if (hostType != HostType.Memory)
            {
                string clientNetworkPath = Path.Combine(logBasePath, testName + ".client.network.log");
                host.Disposables.Add(SystemNetLogging.Enable(clientNetworkPath));
            }

            string testTracePath = Path.Combine(logBasePath, testName + ".test.trace.log");
            var traceListener = new TextWriterTraceListener(testTracePath);
            Trace.Listeners.Add(traceListener);
            Trace.AutoFlush = true;

            host.Disposables.Add(new DisposableAction(() =>
            {
                traceListener.Close();
                Trace.Listeners.Remove(traceListener);
            }));

            EventHandler<UnobservedTaskExceptionEventArgs> handler = (sender, args) =>
            {
                Trace.TraceError("Unobserved task exception: " + args.Exception.GetBaseException());

                Assert.False(true, "Unobserved task exception");
            };

            TaskScheduler.UnobservedTaskException += handler;
            host.Disposables.Add(new DisposableAction(() =>
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }));

            return host;
        }

        protected HubConnection CreateHubConnection(ITestHost host)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            SetHostData(host, query);
            var connection = new HubConnection(host.Url, query);
            connection.Trace = host.ClientTraceOutput ?? connection.Trace;
            return connection;
        }

        protected Client.Connection CreateConnection(ITestHost host, string path)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName();
            SetHostData(host, query);
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
    }
}
