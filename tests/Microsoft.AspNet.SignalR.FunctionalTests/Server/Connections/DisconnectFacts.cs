using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;
using Owin;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;

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
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);
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
        public void DisconnectFiresForPersistentConnectionWhenClientGoesAway()
        {
            using (var host = new MemoryHost())
            {
                var connectWh = new ManualResetEventSlim();
                var disconnectWh = new ManualResetEventSlim();
                var dr = new DefaultDependencyResolver();
                var configuration = dr.Resolve<IConfigurationManager>();

                host.Configure(app =>
                {
                    var config = new ConnectionConfiguration
                    {
                        Resolver = dr
                    };

                    app.MapConnection<MyConnection>("/echo", config);

                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);

                    dr.Register(typeof(MyConnection), () => new MyConnection(connectWh, disconnectWh));
                });
                var connection = new Client.Connection("http://foo/echo");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(configuration.HeartbeatInterval().Ticks * 3);

                connection.Start(host).Wait();

                Assert.True(connectWh.Wait(TimeSpan.FromSeconds(10)), "Connect never fired");

                connection.Stop();

                Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
            }
        }

        [Fact]
        public void DisconnectFiresForHubsWhenConnectionGoesAway()
        {
            using (var host = new MemoryHost())
            {
                var dr = new DefaultDependencyResolver();
                var configuration = dr.Resolve<IConfigurationManager>();

                var connectWh = new ManualResetEventSlim();
                var disconnectWh = new ManualResetEventSlim();
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = dr
                    };

                    app.MapHubs("/signalr", config);

                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    dr.Register(typeof(MyHub), () => new MyHub(connectWh, disconnectWh));
                });

                var connection = new Client.Hubs.HubConnection("http://foo/");

                connection.CreateHubProxy("MyHub");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(configuration.HeartbeatInterval().Ticks * 3);

                connection.Start(host).Wait();

                Assert.True(connectWh.Wait(TimeSpan.FromSeconds(10)), "Connect never fired");

                connection.Stop();

                Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
            }
        }

        [Fact]
        public void FarmDisconnectOnlyRaisesEventOnce()
        {
            EnableTracing();

            // Each node shares the same bus but are indepenent servers
            var counters = new SignalR.Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();
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
                        app.MapConnection<FarmConnection>("/echo", new ConnectionConfiguration
                        {
                            Resolver = resolver
                        });
                    });
                }

                var loadBalancer = new LoadBalancer(nodes.Select(f => f.Server).ToArray());
                var transport = new Client.Transports.LongPollingTransport(loadBalancer);

                var connection = new Client.Connection("http://goo/echo");

                connection.Start(transport).Wait();

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Broadcast(String.Format("From Node {0}: {1}", i, i + 1));
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                ((Client.IConnection)connection).Disconnect();

                Thread.Sleep(TimeSpan.FromTicks(timeout.Ticks * nodes.Count));

                Assert.Equal(1, nodes.Sum(n => n.Connection.DisconnectCount));
            }
        }

        private class ServerNode
        {
            public MemoryHost Server { get; private set; }
            public FarmConnection Connection { get; private set; }
            public IDependencyResolver Resolver { get; private set; }

            private IConnection _connection;

            public ServerNode(IMessageBus bus)
            {
                // Give each server it's own dependency resolver
                Server = new MemoryHost();
                Connection = new FarmConnection();
                Resolver = new DefaultDependencyResolver();

                Resolver.Register(typeof(FarmConnection), () => Connection);
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
                _servers[_counter].Initialize(connection);
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
            public int DisconnectCount { get; set; }

            protected override Task OnDisconnected(IRequest request, string connectionId)
            {
                DisconnectCount++;
                return base.OnDisconnected(request, connectionId);
            }

            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }

        public class MyHub : Hub
        {
            private ManualResetEventSlim _connectWh;
            private ManualResetEventSlim _disconnectWh;

            public MyHub(ManualResetEventSlim connectWh, ManualResetEventSlim disconnectWh)
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
            private ManualResetEventSlim _connectWh;
            private ManualResetEventSlim _disconnectWh;

            public MyConnection(ManualResetEventSlim connectWh, ManualResetEventSlim disconnectWh)
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
