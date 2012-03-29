using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Memory;
using SignalR.Hubs;
using Xunit;

namespace SignalR.Tests
{
    public class DisconnectFacts
    {
        [Fact]
        public void DisconnectFiresForPersistentConnectionWhenClientGoesAway()
        {
            var host = new MemoryHost();
            host.MapConnection<MyConnection>("/echo");
            host.Configuration.DisconnectTimeout = TimeSpan.Zero;
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(5);
            var connectWh = new ManualResetEventSlim();
            var disconnectWh = new ManualResetEventSlim();
            host.DependencyResolver.Register(typeof(MyConnection), () => new MyConnection(connectWh, disconnectWh));
            var connection = new Client.Connection("http://foo/echo");

            // Maximum wait time for disconnect to fire (3 heart beat intervals)
            var disconnectWait = TimeSpan.FromTicks(host.Configuration.HeartBeatInterval.Ticks * 3);

            connection.Start(host).Wait();

            Assert.True(connectWh.Wait(TimeSpan.FromSeconds(100)), "Connect never fired");

            connection.Stop();

            Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
        }

        [Fact]
        public void DisconnectFiresForHubsWhenConnectionGoesAway()
        {
            var host = new MemoryHost();
            host.MapHubs();
            host.Configuration.DisconnectTimeout = TimeSpan.Zero;
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(5);
            var connectWh = new ManualResetEventSlim();
            var disconnectWh = new ManualResetEventSlim();
            host.DependencyResolver.Register(typeof(MyHub), () => new MyHub(connectWh, disconnectWh));
            var connection = new Client.Hubs.HubConnection("http://foo/");

            // Maximum wait time for disconnect to fire (3 heart beat intervals)
            var disconnectWait = TimeSpan.FromTicks(host.Configuration.HeartBeatInterval.Ticks * 3);

            connection.Start(host).Wait();

            Assert.True(connectWh.Wait(TimeSpan.FromSeconds(100)), "Connect never fired");

            connection.Stop();

            Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
        }

        private class MyHub : Hub, IDisconnect, IConnected
        {
            private ManualResetEventSlim _connectWh;
            private ManualResetEventSlim _disconnectWh;

            public MyHub(ManualResetEventSlim connectWh, ManualResetEventSlim disconnectWh)
            {
                _connectWh = connectWh;
                _disconnectWh = disconnectWh;
            }

            public Task Disconnect()
            {
                _disconnectWh.Set();

                return TaskAsyncHelper.Empty;
            }

            public Task Connect()
            {
                _connectWh.Set();

                return TaskAsyncHelper.Empty;
            }

            public Task Reconnect(IEnumerable<string> groups)
            {
                throw new NotImplementedException();
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

            protected override Task OnConnectedAsync(Hosting.IRequest request, string connectionId)
            {
                _connectWh.Set();
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnDisconnectAsync(string connectionId)
            {
                _disconnectWh.Set();
                return base.OnDisconnectAsync(connectionId);
            }
        }
    }
}
