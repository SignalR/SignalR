using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Memory;
using Xunit;

namespace SignalR.Tests
{
    public class PersistentConnectionFacts
    {
        public class OnConnectedAsync
        {
            [Fact]
            public void GroupsAreNotReadOnConnectedAsync()
            {
                var host = new MemoryHost();
                host.MapConnection<MyConnection>("/echo");

                var connection = new Client.Connection("http://foo/echo");
                connection.Groups = new List<string> { typeof(MyConnection).FullName + ".test" };
                connection.Received += data =>
                {
                    Assert.False(true, "Unexpectedly received data");
                };

                connection.Start(host).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }

            [Fact]
            public void GroupsAreNotReadOnConnectedAsyncLongPolling()
            {
                var host = new MemoryHost();
                host.MapConnection<MyConnection>("/echo");

                var connection = new Client.Connection("http://foo/echo");
                connection.Groups = new List<string> { typeof(MyConnection).FullName + ".test" };
                connection.Received += data =>
                {
                    Assert.False(true, "Unexpectedly received data");
                };

                var transport = new Client.Transports.LongPollingTransport(host);
                connection.Start(transport).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }

            [Fact]
            public void SendRaisesOnReceivedFromAllEvents()
            {
                var host = new MemoryHost();
                host.MapConnection<MySendingConnection>("/multisend");

                var connection = new Client.Connection("http://foo/multisend");
                var results = new List<string>();
                connection.Received += data =>
                {
                    results.Add(data);
                };

                connection.Start(host).Wait();
                connection.Send("").Wait();

                Thread.Sleep(TimeSpan.FromSeconds(10));

                Debug.WriteLine(String.Join(", ", results));

                Assert.Equal(4, results.Count);
                Assert.Equal("OnConnectedAsync1", results[0]);
                Assert.Equal("OnConnectedAsync2", results[1]);
                Assert.Equal("OnReceivedAsync1", results[2]);
                Assert.Equal("OnReceivedAsync2", results[3]);
            }
        }

        public class OnReconnectedAsync
        {
            public void ReconnectFiresAfterTimeOutSSE()
            {
                var host = new MemoryHost();
                var conn = new MyReconnect();
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(5);
                host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
                host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                host.MapConnection<MyReconnect>("/endpoint");

                var connection = new Client.Connection("http://foo/endpoint");
                connection.Start(host).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(15));

                Assert.Equal(2, conn.Reconnects);
            }

            public void ReconnectFiresAfterTimeOutLongPolling()
            {
                var host = new MemoryHost();
                var conn = new MyReconnect();
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(5);
                host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
                host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                host.MapConnection<MyReconnect>("/endpoint");

                var connection = new Client.Connection("http://foo/endpoint");
                connection.Start(new Client.Transports.LongPollingTransport(host)).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(15));

                Assert.Equal(2, conn.Reconnects);
            }
        }
    }

    public class MyReconnect : PersistentConnection
    {
        public int Reconnects { get; set; }

        protected override Task OnReconnectedAsync(Hosting.IRequest request, IEnumerable<string> groups, string connectionId)
        {
            Reconnects++;
            return base.OnReconnectedAsync(request, groups, connectionId);
        }
    }

    public class MySendingConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(Hosting.IRequest request, string connectionId)
        {
            Connection.Send(connectionId, "OnConnectedAsync1");
            Connection.Send(connectionId, "OnConnectedAsync2");

            return base.OnConnectedAsync(request, connectionId);
        }

        protected override Task OnReceivedAsync(Hosting.IRequest request, string connectionId, string data)
        {
            Connection.Send(connectionId, "OnReceivedAsync1");
            Connection.Send(connectionId, "OnReceivedAsync2");

            return base.OnReceivedAsync(request, connectionId, data);
        }
    }

    public class MyConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(Hosting.IRequest request, string connectionId)
        {
            return Groups.Send("test", "hey");
        }
    }
}
