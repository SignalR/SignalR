using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Owin;
using Xunit;

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
        public async Task DisconnectFiresForPersistentConnectionWhenClientCallsStop()
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
        public async Task DisconnectFiresForPersistentConnectionWhenClientDisconnects()
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

                await connection.Start(host);

                Assert.True(await connectWh.WaitAsync(TimeSpan.FromSeconds(10)), "Connect never fired");

                ((Client.IConnection)connection).Disconnect();

                Assert.True(await disconnectWh.WaitAsync(TimeSpan.FromSeconds(20)), "Disconnect never fired");
            }
        }

        [Fact]
        public async Task DisconnectFiresForHubsWhenClientCallsStop()
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
        public async Task DisconnectFiresForHubsWhenClientDisconnects()
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

                await connection.Start(host);

                Assert.True(await connectWh.WaitAsync(TimeSpan.FromSeconds(10)), "Connect never fired");

                ((Client.IConnection)connection).Disconnect();

                Assert.True(await disconnectWh.WaitAsync(TimeSpan.FromSeconds(20)), "Disconnect never fired");
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

            public override Task OnDisconnected(bool stopCalled)
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

            protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
            {
                _disconnectWh.Set();
                return base.OnDisconnected(request, connectionId, stopCalled);
            }
        }
    }
}
