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
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
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
            string detailedTestName = GetTestName() + "." + hostType + "." + transportType + "." + Interlocked.Increment(ref _id);

            return HostedTestFactory.CreateHost(hostType, transportType, detailedTestName);
        }

        protected void UseMessageBus(MessageBusType type, IDependencyResolver resolver)
        {
            IMessageBus bus = null;

            switch (type)
            {
                case MessageBusType.Default:
                    break;
                case MessageBusType.Fake:
                    bus = new FakeScaleoutBus(resolver, streams: 1);
                    break;
                case MessageBusType.FakeMultiStream:
                    bus  = new FakeScaleoutBus(resolver, streams: 3);
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

            if (bus != null)
            {
                resolver.Register(typeof(IMessageBus), () => bus);
            }
        }

        protected void EnableTracing()
        {
            string testName = GetTestName();
            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            HostedTestFactory.EnableTracing(GetTestName(), logBasePath);
        }

        protected HubConnection CreateHubConnection(string url)
        {
            string testName = GetTestName();
            var query = new Dictionary<string, string>();
            query["test"] = testName;
            var connection = new HubConnection(url, query);
            connection.TraceWriter = HostedTestFactory.CreateClientTraceWriter(testName);
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
            return HostedTestFactory.CreateTransport(transportType, client);
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
