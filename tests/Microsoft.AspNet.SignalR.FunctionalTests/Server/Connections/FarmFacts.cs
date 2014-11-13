using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Owin;
using Xunit;
using Xunit.Extensions;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;
using IHttpClient = Microsoft.AspNet.SignalR.Client.Http.IHttpClient;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Connections
{
    public class FarmFacts : HostedTest
    {
        [Fact]
        public async Task FarmDisconnectRaisesUncleanDisconnects()
        {
            // Each node shares the same bus but are independent servers
            const int nodeCount = 3;
            var counters = new Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();
            configurationManager.DisconnectTimeout = TimeSpan.FromSeconds(6);

            using (EnableDisposableTracing())
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager, 5000))
            using (var loadBalancer = new LoadBalancer(nodeCount))
            {
                var broadcasters = new List<IConnection>();
                var disconnectCounter = new DisconnectCounter();
                loadBalancer.Configure(app =>
                {
                    var resolver = new DefaultDependencyResolver();

                    resolver.Register(typeof(IMessageBus), () => bus);
                    resolver.Register(typeof(IConfigurationManager), () => configurationManager);
                    resolver.Register(typeof(FarmConnection), () => new FarmConnection(disconnectCounter));

                    var connectionManager = resolver.Resolve<IConnectionManager>();
                    broadcasters.Add(connectionManager.GetConnectionContext<FarmConnection>().Connection);

                    app.MapSignalR<FarmConnection>("/echo", new ConnectionConfiguration
                    {
                        Resolver = resolver
                    });
                });

                var transport = new Client.Transports.LongPollingTransport(loadBalancer);
                var connection = new Client.Connection("http://goo/echo");

                await connection.Start(transport);

                for (int i = 0; i < nodeCount; i++)
                {
                    broadcasters[i].Broadcast(String.Format("From Node {0}: {1}", i, i + 1)).Wait();
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                ((Client.IConnection)connection).Disconnect();

                await Task.Delay(TimeSpan.FromTicks(TimeSpan.FromSeconds(5).Ticks * nodeCount));

                Assert.Equal(0, disconnectCounter.CleanDisconnectCount);
                Assert.Equal(3, disconnectCounter.UncleanDisconnectCount);
            }
        }

        [Theory]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        public async Task FarmGroupAddCompletesSuccessfully(TransportType transportType)
        {
            // https://github.com/SignalR/SignalR/issues/3337
            // Each node shares the same bus but are independent servers
            const int nodeCount = 2;
            var counters = new Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();

            // Ensure /send and /connect requests get handled by different servers
            Func<string, int> scheduler = url => url.Contains("/send") ? 0 : 1;

            using (EnableDisposableTracing())
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager, 5000))
            using (var loadBalancer = new LoadBalancer(nodeCount, scheduler))
            {
                loadBalancer.Configure(app =>
                {
                    var resolver = new DefaultDependencyResolver();
                    resolver.Register(typeof(IMessageBus), () => bus);
                    app.MapSignalR(new HubConfiguration { Resolver = resolver });
                });

                using (var connection = new HubConnection("http://goo/"))
                {
                    var proxy = connection.CreateHubProxy("FarmGroupHub");

                    const string group = "group";
                    const string message = "message";

                    var mre = new AsyncManualResetEvent();
                    proxy.On<string>("message", m =>
                    {
                        if (m == message)
                        {
                            mre.Set();
                        }
                    });

                    Client.Transports.IClientTransport transport;

                    switch (transportType)
                    {
                        case TransportType.LongPolling:
                            transport = new Client.Transports.LongPollingTransport(loadBalancer);
                            break;
                        case TransportType.ServerSentEvents:
                            transport = new Client.Transports.ServerSentEventsTransport(loadBalancer);
                            break;
                        default:
                            throw new ArgumentException("transportType");
                    }

                    await connection.Start(transport);

                    await proxy.Invoke("JoinGroup", group);
                    await proxy.Invoke("SendToGroup", group, message);

                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(5)));
                }
            }
        }

        [Fact]
        public async Task ContextGroupAddCompletesSuccessfully()
        {
            // https://github.com/SignalR/SignalR/issues/3337
            // Each node shares the same bus but are independent servers
            var counters = new Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();

            using (EnableDisposableTracing())
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager, 5000))
            using (var memoryHost = new MemoryHost())
            {
                memoryHost.Configure(app =>
                {
                    var resolver = new DefaultDependencyResolver();
                    resolver.Register(typeof(IMessageBus), () => bus);
                    app.MapSignalR(new HubConfiguration { Resolver = resolver });
                });

                using (var connection = new HubConnection("http://goo/"))
                {
                    var proxy = connection.CreateHubProxy("FarmGroupHub");

                    const string group = "group";
                    const string message = "message";

                    var mre = new AsyncManualResetEvent();
                    proxy.On<string>("message", m =>
                    {
                        if (m == message)
                        {
                            mre.Set();
                        }
                    });

                    await connection.Start(memoryHost);

                    // Add the connection to a group via an IHubContext on a "second" server.
                    var secondResolver = new DefaultDependencyResolver();
                    secondResolver.Register(typeof(IMessageBus), () => bus);
                    var secondConnectionManager = secondResolver.Resolve<IConnectionManager>();
                    var secondHubContext = secondConnectionManager.GetHubContext<FarmGroupHub>();
                    await secondHubContext.Groups.Add(connection.ConnectionId, group);
                    await proxy.Invoke("SendToGroup", group, message);

                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(5)));
                }
            }
        }

        private IDisposable EnableDisposableTracing()
        {
            var traceListener = EnableTracing();

            return new DisposableAction(() =>
            {
                Trace.Listeners.Remove(traceListener);
                traceListener.Close();
                traceListener.Dispose();
            });
        }

        private class LoadBalancer : IHttpClient, IDisposable
        {
            private readonly MemoryHost[] _servers;
            private readonly Func<string, int> _scheduler;

            public void Initialize(SignalR.Client.IConnection connection)
            {
                foreach (IHttpClient server in _servers)
                {
                    server.Initialize(connection);
                }
            }

            public LoadBalancer(int nodeCount)
                : this(nodeCount, null)
            {
                int counter = 0;
                _scheduler = _ => counter++ % nodeCount;
            }

            public LoadBalancer(int nodeCount, Func<string, int> scheduler)
            {
                _servers = new MemoryHost[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    _servers[i] = new MemoryHost();
                }

                _scheduler = scheduler;
            }

            public void Configure(Action<IAppBuilder> startup)
            {
                foreach (var server in _servers)
                {
                    server.Configure(app =>
                    {
                        // Ensure that the connection token is valid on all servers
                        app.Properties[OwinConstants.HostAppNameKey] = "FarmFacts";
                        startup(app);
                    });
                }
            }

            public Task<IClientResponse> Get(string url, Action<IClientRequest> prepareRequest, bool isLongRunning)
            {
                var index = _scheduler(url);
                Debug.WriteLine("Server {0}: GET {1}", index, url);
                return _servers[index].Get(url, prepareRequest, isLongRunning);
            }

            public Task<IClientResponse> Post(string url, Action<IClientRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
            {
                var index = _scheduler(url);
                Debug.WriteLine("Server {0}: POST {1}", index, url);
                return _servers[index].Post(url, prepareRequest, postData, isLongRunning);
            }

            public void Dispose()
            {
                foreach (var server in _servers)
                {
                    server.Dispose();
                }
            }
        }

        private class DisconnectCounter
        {
            public int CleanDisconnectCount { get; set; }
            public int UncleanDisconnectCount { get; set; }
        }

        private class FarmConnection : PersistentConnection
        {
            private readonly DisconnectCounter _disconnectCounter;

            public FarmConnection(DisconnectCounter disconnectCounter)
            {
                _disconnectCounter = disconnectCounter;
            }

            protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
            {
                if (stopCalled)
                {
                    _disconnectCounter.CleanDisconnectCount++;
                }
                else
                {
                    _disconnectCounter.UncleanDisconnectCount++;
                }

                return base.OnDisconnected(request, connectionId, stopCalled);
            }

            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }

        private class FarmGroupHub : Hub
        {
            public async Task JoinGroup(string groupName)
            {
                await Groups.Add(Context.ConnectionId, groupName);
            }

            public async Task SendToGroup(string groupName, string message)
            {
                await Clients.Group(groupName).message(message);
            }
        }
    }
}
