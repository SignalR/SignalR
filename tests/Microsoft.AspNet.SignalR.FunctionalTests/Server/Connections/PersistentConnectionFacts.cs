using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Newtonsoft.Json;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PersistentConnectionFacts
    {
        public class OnConnectedAsync : HostedTest
        {
            [Fact]
            public void ConnectionsWithTheSameConnectionIdSSECloseGracefully()
            {
                using (var host = new MemoryHost())
                {
                    host.MapConnection<MyGroupEchoConnection>("/echo");

                    var tasks = new List<Task>();

                    for (int i = 0; i < 1000; i++)
                    {
                        tasks.Add(ProcessRequest(host, "serverSentEvents", "1"));
                    }

                    ProcessRequest(host, "serverSentEvents", "1");

                    Task.WaitAll(tasks.ToArray());

                    Assert.True(tasks.All(t => !t.IsFaulted));
                }
            }

            [Fact]
            public void ConnectionsWithTheSameConnectionIdLongPollingCloseGracefully()
            {
                using (var host = new MemoryHost())
                {
                    host.MapConnection<MyGroupEchoConnection>("/echo");

                    var tasks = new List<Task>();

                    for (int i = 0; i < 1000; i++)
                    {
                        tasks.Add(ProcessRequest(host, "longPolling", "1"));
                    }

                    ProcessRequest(host, "longPolling", "1");

                    Task.WaitAll(tasks.ToArray());

                    Assert.True(tasks.All(t => !t.IsFaulted));
                }
            }

            private static Task ProcessRequest(MemoryHost host, string transport, string connectionId)
            {
                return host.ProcessRequest("http://foo/echo/connect?transport=" + transport + "&connectionId=" + connectionId, request => { }, null);
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void GroupsAreNotReadOnConnectedAsync(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/group-echo");
                    ((Client.IConnection)connection).Groups.Add(typeof(MyGroupEchoConnection).FullName + ".test");
                    connection.Received += data =>
                    {
                        Assert.False(true, "Unexpectedly received data");
                    };

                    connection.Start(host.Transport).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.Auto)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            // [InlineData(HostType.Memory, TransportType.LongPolling)]
            // [InlineData(HostType.IISExpress, TransportType.Auto)]
            public void GroupCanBeAddedAndMessagedOnConnected(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var wh = new ManualResetEventSlim();
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/add-group");
                    connection.Received += data =>
                    {
                        Assert.Equal("hey", data);
                        wh.Set();
                    };

                    connection.Start(host.Transport).Wait();
                    connection.SendWithTimeout("");

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void SendRaisesOnReceivedFromAllEvents(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/multisend");
                    var results = new List<string>();
                    connection.Received += data =>
                    {
                        results.Add(data);
                    };

                    connection.Start(host.Transport).Wait();
                    connection.SendWithTimeout("");

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Debug.WriteLine(String.Join(", ", results));

                    Assert.Equal(4, results.Count);
                    Assert.Equal("OnConnectedAsync1", results[0]);
                    Assert.Equal("OnConnectedAsync2", results[1]);
                    Assert.Equal("OnReceivedAsync1", results[2]);
                    Assert.Equal("OnReceivedAsync2", results[3]);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void SendCanBeCalledAfterStateChangedEvent(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/multisend");
                    var results = new List<string>();
                    connection.Received += data =>
                    {
                        results.Add(data);
                    };

                    connection.StateChanged += stateChange =>
                    {
                        if (stateChange.NewState == Client.ConnectionState.Connected)
                        {
                            connection.SendWithTimeout("");
                        }
                    };

                    connection.Start(host.Transport).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Debug.WriteLine(String.Join(", ", results));

                    Assert.Equal(4, results.Count);
                }
            }
        }

        public class OnReconnectedAsync : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IIS, TransportType.LongPolling)]
            public void ReconnectFiresAfterHostShutDown(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/my-reconnect");
                    connection.Start(host.Transport).Wait();

                    host.Shutdown();

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    Assert.Equal(Client.ConnectionState.Reconnecting, connection.State);

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(TransportType.ServerSentEvents)]
            [InlineData(TransportType.LongPolling)]
            public void ReconnectFiresAfterTimeOut(TransportType transportType)
            {
                using (var host = new MemoryHost())
                {
                    var conn = new MyReconnect();
                    host.Configuration.KeepAlive = 0;
                    host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(5);
                    host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(5);
                    host.DependencyResolver.Register(typeof(MyReconnect), () => conn);
                    host.MapConnection<MyReconnect>("/endpoint");

                    var connection = new Client.Connection("http://foo/endpoint");
                    var transport = CreateTransport(transportType, host);
                    connection.Start(transport).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(15));

                    connection.Stop();

                    Assert.InRange(conn.Reconnects, 1, 4);
                }
            }
        }

        public class GroupTest : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            public void GroupsReceiveMessages(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/groups");
                    var list = new List<string>();
                    connection.Received += data =>
                    {
                        list.Add(data);
                    };

                    connection.Start(host.Transport).Wait();

                    // Join the group
                    connection.SendWithTimeout(new { type = 1, group = "test" });

                    // Sent a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "hello to group test" });

                    // Leave the group
                    connection.SendWithTimeout(new { type = 2, group = "test" });

                    // Send a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "goodbye to group test" });

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Assert.Equal(1, list.Count);
                    Assert.Equal("hello to group test", list[0]);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            public void GroupsDontRejoinByDefault(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: 0,
                                    connectionTimeout: 2,
                                    hearbeatInterval: 2);

                    var connection = new Client.Connection(host.Url + "/groups");
                    var list = new List<string>();
                    connection.Received += data =>
                    {
                        list.Add(data);
                    };

                    connection.Start(host.Transport).Wait();

                    // Join the group
                    connection.SendWithTimeout(new { type = 1, group = "test" });

                    // Sent a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "hello to group test" });

                    // Force Reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    // Send a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "goodbye to group test" });

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Assert.Equal(1, list.Count);
                    Assert.Equal("hello to group test", list[0]);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void GroupsRejoinedWhenOnRejoiningGroupsOverridden(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: 0,
                                    connectionTimeout: 2,
                                    hearbeatInterval: 2);

                    var connection = new Client.Connection(host.Url + "/rejoin-groups");

                    var list = new List<string>();
                    connection.Received += data =>
                    {
                        list.Add(data);
                    };

                    connection.Start(host.Transport).Wait();

                    // Join the group
                    connection.SendWithTimeout(new { type = 1, group = "test" });

                    // Sent a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "hello to group test" });

                    // Force Reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    // Send a message
                    connection.SendWithTimeout(new { type = 3, group = "test", message = "goodbye to group test" });

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Assert.Equal(2, list.Count);
                    Assert.Equal("hello to group test", list[0]);
                    Assert.Equal("goodbye to group test", list[1]);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void ClientGroupsSyncWithServerGroupsOnReconnect(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: 0,
                                    connectionTimeout: 5,
                                    hearbeatInterval: 2);

                    var connection = new Client.Connection(host.Url + "/rejoin-groups");
                    var inGroupOnReconnect = new List<bool>();
                    var wh = new ManualResetEventSlim();

                    connection.Received += message =>
                    {
                        Assert.Equal("Reconnected", message);
                        wh.Set();
                    };

                    connection.Reconnected += () =>
                    {
                        inGroupOnReconnect.Add(connection.Groups.Contains(typeof(MyRejoinGroupsConnection).FullName + ".test"));

                        connection.SendWithTimeout(new { type = 3, group = "test", message = "Reconnected" });
                    };

                    connection.Start(host.Transport).Wait();

                    // Join the group
                    connection.SendWithTimeout(new { type = 1, group = "test" });

                    // Force reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(10));

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)), "Client didn't receive message sent to test group.");
                    Assert.True(inGroupOnReconnect.Count > 0);
                    Assert.True(inGroupOnReconnect.All(b => b));

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            public void ClientGroupsSyncWithServerGroupsOnReconnectWhenNotRejoiningGroups(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: 0,
                                    connectionTimeout: 5,
                                    hearbeatInterval: 2);

                    var connection = new Client.Connection(host.Url + "/groups");
                    var inGroupOnReconnect = new List<bool>();
                    var wh = new ManualResetEventSlim();

                    connection.Received += message =>
                    {
                        Assert.Equal("Reconnected", message);
                        inGroupOnReconnect.Add(!connection.Groups.Contains(typeof(MyGroupConnection).FullName + ".test"));
                        inGroupOnReconnect.Add(connection.Groups.Contains(typeof(MyGroupConnection).FullName + ".test2"));
                        wh.Set();
                    };

                    connection.Reconnected += () =>
                    {
                        connection.SendWithTimeout(new { type = 1, group = "test2" });
                        connection.SendWithTimeout(new { type = 3, group = "test2", message = "Reconnected" });
                    };

                    connection.Start(host.Transport).Wait();

                    // Join the group
                    connection.SendWithTimeout(new { type = 1, group = "test" });

                    // Force reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)), "Client didn't receive message sent to test group.");
                    Assert.True(inGroupOnReconnect.Count > 0);
                    Assert.True(inGroupOnReconnect.All(b => b));

                    connection.Stop();
                }
            }
        }

        public class SendFacts : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void SendToAllButCaller(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection1 = new Client.Connection(host.Url + "/filter");
                    var connection2 = new Client.Connection(host.Url + "/filter");

                    var wh1 = new ManualResetEventSlim(initialState: false);
                    var wh2 = new ManualResetEventSlim(initialState: false);

                    connection1.Received += data => wh1.Set();
                    connection2.Received += data => wh2.Set();

                    connection1.Start(host.Transport).Wait();
                    connection2.Start(host.Transport).Wait();

                    connection1.SendWithTimeout("test");

                    Assert.False(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                    Assert.True(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));

                    connection1.Stop();
                    connection2.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void SendWithSyncErrorThrows(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/sync-error");

                    connection.Start(host.Transport).Wait();

                    Assert.Throws<AggregateException>(() => connection.SendWithTimeout("test"));

                    connection.Stop();
                }
            }
        }

        public class Owin : HostedTest
        {
            [Theory]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void EnvironmentIsAvailable(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = new Client.Connection(host.Url + "/items");
                    var connection2 = new Client.Connection(host.Url + "/items");

                    var results = new List<RequestItemsResponse>();
                    connection2.Received += data =>
                    {
                        var val = JsonConvert.DeserializeObject<RequestItemsResponse>(data);
                        if (!results.Contains(val))
                        {
                            results.Add(val);
                        }
                    };
 
                    connection.Start(host.Transport).Wait();
                    connection2.Start(host.Transport).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    connection.SendWithTimeout(null);

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    connection.Stop();

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    Debug.WriteLine(String.Join(", ", results));

                    Assert.Equal(3, results.Count);
                    Assert.Equal("OnConnectedAsync", results[0].Method);
                    Assert.Equal(1, results[0].Keys.Length);
                    Assert.Equal("owin.environment", results[0].Keys[0]);
                    Assert.Equal("OnReceivedAsync", results[1].Method);
                    Assert.Equal(1, results[1].Keys.Length);
                    Assert.Equal("owin.environment", results[1].Keys[0]);
                    Assert.Equal("OnDisconnectAsync", results[2].Method);
                    Assert.Equal(1, results[2].Keys.Length);
                    Assert.Equal("owin.environment", results[2].Keys[0]);

                    connection2.Stop();
                }
            }
        }
    }
}
