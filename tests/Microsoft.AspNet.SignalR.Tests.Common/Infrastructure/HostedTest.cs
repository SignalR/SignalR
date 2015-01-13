using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
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

        protected void UseMessageBus(MessageBusType type, IDependencyResolver resolver, int streams = 1)
        {
            IMessageBus bus = null;

            switch (type)
            {
                case MessageBusType.Default:
                    break;
                case MessageBusType.Fake:
                case MessageBusType.FakeMultiStream:
                    bus = new FakeScaleoutBus(resolver, streams);
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

        protected void SetReconnectDelay(IClientTransport transport, TimeSpan delay)
        {
            // SUPER ugly, alternative is adding an overload to the create host function, adding a member to the
            // IClientTransport object or using Reflection.  Adding a member to IClientTransport isn't horrible
            // but we want to avoid making a breaking change... Therefore this is the least of the evils.
            if (transport is ServerSentEventsTransport)
            {
                (transport as ServerSentEventsTransport).ReconnectDelay = delay;
            }
            else if (transport is LongPollingTransport)
            {
                (transport as LongPollingTransport).ReconnectDelay = delay;
            }
            else if (transport is WebSocketTransport)
            {
                (transport as WebSocketTransport).ReconnectDelay = delay;
            }
        }

        protected TextWriterTraceListener EnableTracing()
        {
            string testName = GetTestName() + "." + Interlocked.Increment(ref _id);
            string logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            return HostedTestFactory.EnableTracing(testName, logBasePath);
        }

        protected HubConnection CreateHubConnection(string url)
        {
            string testName = GetTestName() + "." + Interlocked.Increment(ref _id);
            var query = new Dictionary<string, string>();
            query["test"] = testName;
            var connection = new HubConnection(url, query);
            connection.TraceWriter = HostedTestFactory.CreateClientTraceWriter(testName);
            return connection;
        }

        protected Connection CreateConnection(string url)
        {
            string testName = GetTestName() + "." + Interlocked.Increment(ref _id);
            var query = new Dictionary<string, string>();
            query["test"] = testName;
            var connection = new Connection(url, query);
            connection.TraceWriter = HostedTestFactory.CreateClientTraceWriter(testName);
            return connection;
        }

        protected HubConnection CreateHubConnection(ITestHost host, string path = null, bool useDefaultUrl = true)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName() + "." + Interlocked.Increment(ref _id);
            SetHostData(host, query);
            var connection = new HubConnection(host.Url + path, query, useDefaultUrl);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
            return connection;
        }

        protected HubConnection CreateAuthHubConnection(ITestHost host, string user, string password)
        {
            var path = "/cookieauth/signalr";
            var useDefaultUrl = false;
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName() + "." + Interlocked.Increment(ref _id);
            SetHostData(host, query);

            var handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            using (var httpClient = new HttpClient(handler))
            {
                var content = string.Format("UserName={0}&Password={1}", user, password);
                var response = httpClient.PostAsync(host.Url + "/cookieauth/Account/Login", new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")).Result;
            }

            var connection = new HubConnection(host.Url + path, query, useDefaultUrl);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
            connection.CookieContainer = handler.CookieContainer;
            return connection;
        }

        protected Client.Connection CreateConnection(ITestHost host, string path)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName() + "." + Interlocked.Increment(ref _id);
            SetHostData(host, query);
            var connection = new Client.Connection(host.Url + path, query);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
            return connection;
        }

        protected Client.Connection CreateAuthConnection(ITestHost host, string path, string user, string password)
        {
            var query = new Dictionary<string, string>();
            query["test"] = GetTestName() + "." + Interlocked.Increment(ref _id);
            SetHostData(host, query);

            var handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            using (var httpClient = new HttpClient(handler))
            {
                var content = string.Format("UserName={0}&Password={1}", user, password);
                var response = httpClient.PostAsync(host.Url + "/cookieauth/Account/Login", new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")).Result;
            }
            
            var connection = new Client.Connection(host.Url + "/cookieauth" + path, query);
            connection.TraceWriter = host.ClientTraceOutput ?? connection.TraceWriter;
            connection.CookieContainer = handler.CookieContainer;
            
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
