using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

                connection.Stop();
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

                connection.Stop();
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

                connection.Stop();

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
            [Fact]
            public void ReconnectFiresAfterHostShutDown()
            {
                var host = new MemoryHost();
                var conn = new MyReconnect();
                host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                host.MapConnection<MyReconnect>("/endpoint");

                var connection = new Client.Connection("http://foo/endpoint");
                connection.Start(host).Wait();

                host.Dispose();

                Thread.Sleep(TimeSpan.FromSeconds(5));

                Assert.Equal(Client.ConnectionState.Reconnecting, connection.State);

                connection.Stop();
            }

            [Fact]
            public void ReconnectFiresAfterTimeOutSSE()
            {
                var host = new MemoryHost();
                var conn = new MyReconnect();
                host.Configuration.KeepAlive = null;
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(5);
                host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(5);
                host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                host.MapConnection<MyReconnect>("/endpoint");

                var connection = new Client.Connection("http://foo/endpoint");
                connection.Start(new Client.Transports.ServerSentEventsTransport(host)).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(15));

                connection.Stop();

                Assert.InRange(conn.Reconnects, 1, 4);
            }

            [Fact]
            public void ReconnectFiresAfterTimeOutLongPolling()
            {
                var host = new MemoryHost();
                var conn = new MyReconnect();
                host.Configuration.KeepAlive = null;
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(5);
                host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(5);
                host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                host.MapConnection<MyReconnect>("/endpoint");

                var connection = new Client.Connection("http://foo/endpoint");
                connection.Start(new Client.Transports.LongPollingTransport(host)).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(15));

                connection.Stop();

                Assert.InRange(conn.Reconnects, 1, 4);
            }
        }

        public class GroupTest
        {
            [Fact]
            public void GroupsReceiveMessages()
            {
                var host = new MemoryHost();
                host.MapConnection<MyGroupConnection>("/groups");

                var connection = new Client.Connection("http://foo/groups");
                var list = new List<string>();
                connection.Received += data =>
                {
                    list.Add(data);
                };

                connection.Start(host).Wait();

                // Join the group
                connection.Send(new { type = 1, group = "test" }).Wait();

                // Sent a message
                connection.Send(new { type = 3, group = "test", message = "hello to group test" }).Wait();

                // Leave the group
                connection.Send(new { type = 2, group = "test" }).Wait();

                // Send a message
                connection.Send(new { type = 3, group = "test", message = "goodbye to group test" }).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(15));

                connection.Stop();

                Assert.Equal(1, list.Count);
                Assert.Equal("hello to group test", list[0]);
            }
        }
    }

    public class MyGroupConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            JObject operation = JObject.Parse(data);
            int type = operation.Value<int>("type");
            string group = operation.Value<string>("group");

            if (type == 1)
            {
                return Groups.Add(connectionId, group);
            }
            else if (type == 2)
            {
                return Groups.Remove(connectionId, group);
            }
            else if (type == 3)
            {
                return Groups.Send(group, operation.Value<string>("message"));
            }

            return base.OnReceivedAsync(request, connectionId, data);
        }
    }

    public class MyReconnect : PersistentConnection
    {
        public int Reconnects { get; set; }

        protected override Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            Reconnects++;
            return base.OnReconnectedAsync(request, groups, connectionId);
        }
    }

    public class MySendingConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            Connection.Send(connectionId, "OnConnectedAsync1");
            Connection.Send(connectionId, "OnConnectedAsync2");

            return base.OnConnectedAsync(request, connectionId);
        }

        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            Connection.Send(connectionId, "OnReceivedAsync1");
            Connection.Send(connectionId, "OnReceivedAsync2");

            return base.OnReceivedAsync(request, connectionId, data);
        }
    }

    public class MyConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return Groups.Send("test", "hey");
        }
    }
}
