// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
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
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;
using IHttpClient = Microsoft.AspNet.SignalR.Client.Http.IHttpClient;
using PerformanceCounterManager = Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Connections
{
    public class FarmFacts : HostedTest
    {
        [Fact]
        public async Task FarmDisconnectRaisesUncleanDisconnects()
        {
            // Each node shares the same bus but are independent servers
            const int nodeCount = 3;
            var counters = new PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();
            configurationManager.DisconnectTimeout = TimeSpan.FromSeconds(6);

            using (EnableTracing())
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager, 5000))
            using (var loadBalancer = new LoadBalancer(nodeCount))
            {
                var broadcasters = new List<IConnection>();
                var disconnectCounter = Channel.CreateUnbounded<DisconnectData>();
                loadBalancer.Configure(app =>
                {
                    var resolver = new DefaultDependencyResolver();

                    resolver.Register(typeof(IMessageBus), () => bus);
                    resolver.Register(typeof(IConfigurationManager), () => configurationManager);
                    resolver.Register(typeof(FarmConnection), () => new FarmConnection(disconnectCounter.Writer));

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
                    await broadcasters[i].Broadcast(String.Format("From Node {0}: {1}", i, i + 1)).OrTimeout();
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                ((Client.IConnection)connection).Disconnect();

                // Give up after 30 seconds
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                // We can get duplicate OnDisconnected calls, and that's a known by-design issue.
                var instancesDisconnected = new HashSet<string>();
                while (await disconnectCounter.Reader.WaitToReadAsync(cts.Token))
                {
                    while (!cts.IsCancellationRequested && disconnectCounter.Reader.TryRead(out var disconnect))
                    {
                        Assert.False(disconnect.StopCalled, "Disconnect should not have been due to stop being called.");
                        instancesDisconnected.Add(disconnect.InstanceName);
                        if (instancesDisconnected.Count == 3)
                        {
                            // We're done, all three instances disconneted
                            return;
                        }
                    }
                }

                // If we get here it means the cts was cancelled which means we timed out
                cts.Token.ThrowIfCancellationRequested();
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
            var counters = new PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();

            // Ensure /send and /connect requests get handled by different servers
            Func<string, int> scheduler = url => url.Contains("/send") ? 0 : 1;

            using (EnableTracing())
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

                    var mre = new TaskCompletionSource<object>();
                    proxy.On<string>("message", m =>
                    {
                        if (m == message)
                        {
                            mre.TrySetResult(null);
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

                    await mre.Task.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task ContextGroupAddCompletesSuccessfully()
        {
            // https://github.com/SignalR/SignalR/issues/3337
            // Each node shares the same bus but are independent servers
            var counters = new PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();

            using (EnableTracing())
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

                    var mre = new TaskCompletionSource<object>();
                    proxy.On<string>("message", m =>
                    {
                        if (m == message)
                        {
                            mre.TrySetResult(null);
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

                    await mre.Task.OrTimeout();
                }
            }
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

        private class DisconnectData
        {
            public bool StopCalled { get; }
            public string InstanceName { get; }

            public DisconnectData(bool stopCalled, string instanceName)
            {
                StopCalled = stopCalled;
                InstanceName = instanceName;
            }
        }

        private class FarmConnection : PersistentConnection
        {
            private readonly ChannelWriter<DisconnectData> _disconnectCounter;

            public FarmConnection(ChannelWriter<DisconnectData> disconnectCounter)
            {
                _disconnectCounter = disconnectCounter;
            }

            protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
            {
                var data = new DisconnectData(stopCalled, request.Environment.Get<string>(MemoryHost.InstanceNameKey));
                Assert.True(_disconnectCounter.TryWrite(data), "Disconnect counter channel should be unbounded!");

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
