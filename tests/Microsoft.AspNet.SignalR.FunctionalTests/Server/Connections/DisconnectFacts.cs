﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class DisconnectFacts : IDisposable
    {
        [Fact]
        public void FailedWriteCompletesRequestAfterDisconnectTimeout()
        {
            var request = new Mock<IRequest>();
            var response = new Mock<IResponse>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            response.Setup(m => m.EndAsync()).Returns(TaskAsyncHelper.Empty);
            bool isConnected = true;
            response.Setup(m => m.IsClientConnected).Returns(() => isConnected);
            response.Setup(m => m.FlushAsync()).Returns(TaskAsyncHelper.Empty);

            var resolver = new DefaultDependencyResolver();
            var config = resolver.Resolve<IConfigurationManager>();
            var hostContext = new HostContext(request.Object, response.Object);
            config.DisconnectTimeout = TimeSpan.Zero;
            config.HeartbeatInterval = TimeSpan.FromSeconds(3);
            var transport = new Mock<ForeverTransport>(hostContext, resolver)
            {
                CallBase = true
            };

            transport.Setup(m => m.Send(It.IsAny<PersistentResponse>()))
                     .Returns(() =>
                     {
                         var task = TaskAsyncHelper.FromError(new Exception());
                         isConnected = false;
                         return task;
                     });

            var connectionManager = new ConnectionManager(resolver);
            var connection = connectionManager.GetConnection("Foo");
            var wh = new ManualResetEventSlim();

            transport.Object.ProcessRequest(connection).ContinueWith(task =>
            {
                wh.Set();
            });

            connection.Broadcast("Some message");

            Assert.True(wh.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void DisconnectFiresForPersistentConnectionWhenClientGoesAway()
        {
            using (var host = new MemoryHost())
            {
                host.MapConnection<MyConnection>("/echo");
                host.Configuration.DisconnectTimeout = TimeSpan.Zero;
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(5);
                var connectWh = new ManualResetEventSlim();
                var disconnectWh = new ManualResetEventSlim();
                host.DependencyResolver.Register(typeof(MyConnection), () => new MyConnection(connectWh, disconnectWh));
                var connection = new Client.Connection("http://foo/echo");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(host.Configuration.HeartbeatInterval.Ticks * 3);

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
                host.MapHubs();
                host.Configuration.DisconnectTimeout = TimeSpan.Zero;
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(5);
                var connectWh = new ManualResetEventSlim();
                var disconnectWh = new ManualResetEventSlim();
                host.DependencyResolver.Register(typeof(MyHub), () => new MyHub(connectWh, disconnectWh));
                var connection = new Client.Hubs.HubConnection("http://foo/");

                connection.CreateHubProxy("MyHub");

                // Maximum wait time for disconnect to fire (3 heart beat intervals)
                var disconnectWait = TimeSpan.FromTicks(host.Configuration.HeartbeatInterval.Ticks * 3);

                connection.Start(host).Wait();

                Assert.True(connectWh.Wait(TimeSpan.FromSeconds(10)), "Connect never fired");

                connection.Stop();

                Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
            }
        }

        [Fact]
        public void FarmDisconnectOnlyRaisesEventOnce()
        {
            // Each node shares the same bus but are indepenent servers
            var counters = new SignalR.Infrastructure.PerformanceCounterManager();
            var configurationManager = new DefaultConfigurationManager();
            using (var bus = new MessageBus(new StringMinifier(), new TraceManager(), counters, configurationManager))
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
                    node.Server.Configuration.HeartbeatInterval = timeout;
                    node.Server.Configuration.DisconnectTimeout = TimeSpan.Zero;
                    node.Server.MapConnection<FarmConnection>("/echo");
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

                connection.Disconnect();

                Thread.Sleep(TimeSpan.FromTicks(timeout.Ticks * nodes.Count));

                Assert.Equal(1, nodes.Sum(n => n.Connection.DisconnectCount));
            }
        }

        private class ServerNode
        {
            public MemoryHost Server { get; private set; }
            public FarmConnection Connection { get; private set; }

            private IConnection _connection;

            public ServerNode(IMessageBus bus)
            {
                // Give each server it's own dependency resolver
                Server = new MemoryHost(new DefaultDependencyResolver());
                Connection = new FarmConnection();

                Server.DependencyResolver.Register(typeof(FarmConnection), () => Connection);
                Server.DependencyResolver.Register(typeof(IMessageBus), () => bus);

                var context = Server.ConnectionManager.GetConnectionContext<FarmConnection>();
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
            public LoadBalancer(params SignalR.Client.Http.IHttpClient[] servers)
            {
                _servers = servers;
            }

            public Task<IClientResponse> GetAsync(string url, Action<IClientRequest> prepareRequest)
            {
                Debug.WriteLine("Server {0}: GET {1}", _counter, url);
                int index = _counter;
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].GetAsync(url, prepareRequest);
            }

            public Task<IClientResponse> PostAsync(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData)
            {
                Debug.WriteLine("Server {0}: POST {1}", _counter, url);
                int index = _counter;
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].PostAsync(url, prepareRequest, postData);
            }
        }

        private class FarmConnection : PersistentConnection
        {
            public int DisconnectCount { get; set; }

            protected override Task OnDisconnectAsync(IRequest request, string connectionId)
            {
                DisconnectCount++;
                return base.OnDisconnectAsync(request, connectionId);
            }

            protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
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

            protected override Task OnConnectedAsync(IRequest request, string connectionId)
            {
                _connectWh.Set();
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnDisconnectAsync(IRequest request, string connectionId)
            {
                _disconnectWh.Set();
                return base.OnDisconnectAsync(request, connectionId);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
