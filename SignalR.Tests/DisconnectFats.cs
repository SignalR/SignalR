using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Memory;
using Xunit;

namespace SignalR.Tests
{
    public class DisconnectFats
    {
        [Fact]
        public void DisconnectFiresForPersistentConnectionWhenClientGoesAway()
        {
            var host = new MemoryHost();
            host.MapConnection<MyConnection>("/echo");
            var configurationManager = host.DependencyResolver.Resolve<IConfigurationManager>();
            configurationManager.DisconnectTimeout = TimeSpan.Zero;
            configurationManager.HeartBeatInterval = TimeSpan.FromSeconds(5);
            var connectWh = new ManualResetEventSlim();
            var disconnectWh = new ManualResetEventSlim();
            host.DependencyResolver.Register(typeof(MyConnection), () => new MyConnection(connectWh, disconnectWh));
            var connection = new Client.Connection("http://foo/echo");

            // Maximum wait time for disconnect to fire (2 heart beat intervals)
            var disconnectWait = configurationManager.HeartBeatInterval + configurationManager.HeartBeatInterval;

            connection.Start(host).Wait();

            Assert.True(connectWh.Wait(TimeSpan.FromSeconds(100)), "Connect never fired");

            connection.Stop();

            Assert.True(disconnectWh.Wait(disconnectWait), "Disconnect never fired");
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
