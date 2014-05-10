﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Owin;
using Xunit;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class DisconnectFacts : HostedTest
    {
        [Fact]
        public void FailedWriteCompletesRequestAfterDisconnectTimeout()
        {
            var request = new Mock<IRequest>();
            var response = new Mock<IResponse>();
            var qs = new NameValueCollection();
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            var url = new Uri("http://test/echo/connect");
            request.Setup(m => m.Url).Returns(url);
            request.Setup(m => m.LocalPath).Returns(url.LocalPath);
            var cts = new CancellationTokenSource();
            response.Setup(m => m.CancellationToken).Returns(cts.Token);
            response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

            var resolver = new DefaultDependencyResolver();
            var config = resolver.Resolve<IConfigurationManager>();
            var hostContext = new HostContext(request.Object, response.Object);
            config.DisconnectTimeout = TimeSpan.FromSeconds(6);
            var transport = new Mock<ForeverTransport>(hostContext, resolver)
            {
                CallBase = true
            };
            transport.Object.ConnectionId = "1";
            transport.Setup(m => m.Send(It.IsAny<PersistentResponse>()))
                     .Returns(() =>
                     {
                         var task = TaskAsyncHelper.FromError(new Exception());
                         cts.Cancel();
                         return task;
                     });

            var connectionManager = new ConnectionManager(resolver);
            var connection = connectionManager.GetConnectionCore("Foo");
            var wh = new ManualResetEventSlim();

            transport.Object.ProcessRequest(connection).ContinueWith(task =>
            {
                wh.Set();
            });

            connection.Broadcast("Some message");

            // 6 second disconnect timeout + 5 second disconnect threshold
            // + up to 1 second for the heartbeat to check + 3 second leeway
            Assert.True(wh.Wait(TimeSpan.FromSeconds(15)));
        }

        [Fact]
        public async Task DisconnectFiresForPersistentConnectionWhenClientGoesAway()
        {
            using (var host = new MemoryHost())
            {
                var connectWh = new AsyncManualResetEvent();
                var disconnectWh = new AsyncManualResetEvent();
                var dr = new DefaultDependencyResolver();
                var configuration = dr.Resolve<IConfigurationManager>();

                host.Configure(app =>
                {
                    var config = new ConnectionConfiguration
                    {
                        Resolver = dr
                    };

                    app.MapSignalR<MyConnection>("/echo", config);

                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);

                    dr.Register(typeof(MyConnection), () => new MyConnection(connectWh, disconnectWh));
                });
                var connection = new Client.Connection("http://foo/echo");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(configuration.HeartbeatInterval().Ticks * 3);

                await connection.Start(host);

                Assert.True(await connectWh.WaitAsync(TimeSpan.FromSeconds(10)), "Connect never fired");

                connection.Stop();

                Assert.True(await disconnectWh.WaitAsync(disconnectWait), "Disconnect never fired");
            }
        }

        [Fact]
        public async Task DisconnectFiresForHubsWhenConnectionGoesAway()
        {
            using (var host = new MemoryHost())
            {
                var dr = new DefaultDependencyResolver();
                var configuration = dr.Resolve<IConfigurationManager>();

                var connectWh = new AsyncManualResetEvent();
                var disconnectWh = new AsyncManualResetEvent();
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = dr
                    };

                    app.MapSignalR("/signalr", config);

                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    dr.Register(typeof(MyHub), () => new MyHub(connectWh, disconnectWh));
                });

                var connection = new HubConnection("http://foo/");

                connection.CreateHubProxy("MyHub");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(configuration.HeartbeatInterval().Ticks * 3);

                await connection.Start(host);

                Assert.True(await connectWh.WaitAsync(TimeSpan.FromSeconds(10)), "Connect never fired");

                connection.Stop();

                Assert.True(await disconnectWh.WaitAsync(disconnectWait), "Disconnect never fired");
            }
        }

        [Fact]
        public async Task FarmDisconnectOnlyRaisesUncleanDisconnects()
        {
            EnableTracing();

            // Each node shares the same bus but are indepenent servers
            var counters = new SignalR.Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();
            var protectedData = new DefaultProtectedData();
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager, 5000))
            {
                var nodeCount = 3;
                var nodes = new List<ServerNode>();
                for (int i = 0; i < nodeCount; i++)
                {
                    nodes.Add(new ServerNode(bus));
                }

                var timeout = TimeSpan.FromSeconds(5);
                foreach (var node in nodes)
                {
                    var config = node.Resolver.Resolve<IConfigurationManager>();
                    config.DisconnectTimeout = TimeSpan.FromSeconds(6);

                    IDependencyResolver resolver = node.Resolver;
                    node.Server.Configure(app =>
                    {
                        app.MapSignalR<FarmConnection>("/echo", new ConnectionConfiguration
                        {
                            Resolver = resolver
                        });

                        resolver.Register(typeof(IProtectedData), () => protectedData);
                    });
                }

                var loadBalancer = new LoadBalancer(nodes.Select(f => f.Server).ToArray());
                var transport = new Client.Transports.LongPollingTransport(loadBalancer);

                var connection = new Client.Connection("http://goo/echo");

                await connection.Start(transport);

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Broadcast(String.Format("From Node {0}: {1}", i, i + 1));
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                ((Client.IConnection)connection).Disconnect();

                await Task.Delay(TimeSpan.FromTicks(timeout.Ticks * nodes.Count));

                Assert.Equal(0, FarmConnection.OldOnDisconnectedCalls);
                Assert.Equal(0, FarmConnection.CleanDisconnectCount);
                Assert.Equal(3, FarmConnection.UncleanDisconnectCount);
            }
        }

        private class ServerNode
        {
            public MemoryHost Server { get; private set; }
            public IDependencyResolver Resolver { get; private set; }

            private IConnection _connection;

            public ServerNode(IMessageBus bus)
            {
                // Give each server it's own dependency resolver
                Server = new MemoryHost();
                Resolver = new DefaultDependencyResolver();

                Resolver.Register(typeof(FarmConnection), () => new FarmConnection());
                Resolver.Register(typeof(IMessageBus), () => bus);

                var context = Resolver.Resolve<IConnectionManager>().GetConnectionContext<FarmConnection>();
                _connection = context.Connection;
            }

            public void Broadcast(string message)
            {
                _connection.Broadcast(message).Wait();
            }
        }

        private class LoadBalancer : SignalR.Client.Http.IHttpClient
        {
            private int _counter;
            private readonly SignalR.Client.Http.IHttpClient[] _servers;

            public void Initialize(SignalR.Client.IConnection connection)
            {
                foreach (SignalR.Client.Http.IHttpClient server in _servers)
                {
                    server.Initialize(connection);
                }
            }

            public LoadBalancer(params SignalR.Client.Http.IHttpClient[] servers)
            {
                _servers = servers;
            }

            public Task<IClientResponse> Get(string url, Action<IClientRequest> prepareRequest, bool isLongRunning)
            {
                Debug.WriteLine("Server {0}: GET {1}", _counter, url);
                int index = _counter;
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].Get(url, prepareRequest, isLongRunning);
            }

            public Task<IClientResponse> Post(string url, Action<IClientRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
            {
                Debug.WriteLine("Server {0}: POST {1}", _counter, url);
                int index = _counter;
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].Post(url, prepareRequest, postData, isLongRunning); ;
            }
        }

        private class FarmConnection : PersistentConnection
        {
            public static int CleanDisconnectCount { get; set; }
            public static int UncleanDisconnectCount { get; set; }
            public static int OldOnDisconnectedCalls { get; set; }

            protected override Task OnDisconnected(IRequest request, string connectionId)
            {
                OldOnDisconnectedCalls++;
                return base.OnDisconnected(request, connectionId);
            }

            protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
            {
                if (stopCalled)
                {
                    CleanDisconnectCount++;
                }
                else
                {
                    UncleanDisconnectCount++;
                }

                return base.OnDisconnected(request, connectionId, stopCalled);
            }

            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }

        public class MyHub : Hub
        {
            private AsyncManualResetEvent _connectWh;
            private AsyncManualResetEvent _disconnectWh;

            public MyHub(AsyncManualResetEvent connectWh, AsyncManualResetEvent disconnectWh)
            {
                _connectWh = connectWh;
                _disconnectWh = disconnectWh;
            }

            public override Task OnDisconnected()
            {
                _disconnectWh.Set();

                return null;
            }

            public override Task OnConnected()
            {
                _connectWh.Set();

                return TaskAsyncHelper.Empty;
            }

            public override Task OnReconnected()
            {
                return TaskAsyncHelper.Empty;
            }
        }

        private class MyConnection : PersistentConnection
        {
            private AsyncManualResetEvent _connectWh;
            private AsyncManualResetEvent _disconnectWh;

            public MyConnection(AsyncManualResetEvent connectWh, AsyncManualResetEvent disconnectWh)
            {
                _connectWh = connectWh;
                _disconnectWh = disconnectWh;
            }

            protected override Task OnConnected(IRequest request, string connectionId)
            {
                _connectWh.Set();
                return base.OnConnected(request, connectionId);
            }

            protected override Task OnDisconnected(IRequest request, string connectionId)
            {
                _disconnectWh.Set();
                return base.OnDisconnected(request, connectionId);
            }
        }
    }
}
