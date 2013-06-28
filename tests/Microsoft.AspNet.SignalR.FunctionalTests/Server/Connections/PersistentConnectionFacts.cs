using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
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
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapConnection<MyGroupEchoConnection>("/echo", config);

                        config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
                    });

                    string id = Guid.NewGuid().ToString("d");

                    var tasks = new List<Task>();

                    for (int i = 0; i < 1000; i++)
                    {
                        tasks.Add(ProcessRequest(host, "serverSentEvents", id));
                    }

                    ProcessRequest(host, "serverSentEvents", id);

                    Task.WaitAll(tasks.ToArray());

                    Assert.True(tasks.All(t => !t.IsFaulted));
                }
            }

            [Fact]
            public void ConnectionsWithTheSameConnectionIdLongPollingCloseGracefully()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapConnection<MyGroupEchoConnection>("/echo", config);

                        config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
                    });

                    string id = Guid.NewGuid().ToString("d");

                    var tasks = new List<Task>();

                    for (int i = 0; i < 1000; i++)
                    {
                        tasks.Add(ProcessRequest(host, "longPolling", id));
                    }

                    ProcessRequest(host, "longPolling", id);

                    Task.WaitAll(tasks.ToArray());

                    Assert.True(tasks.All(t => !t.IsFaulted));
                }
            }

            private static Task ProcessRequest(MemoryHost host, string transport, string connectionToken)
            {
                return host.Get("http://foo/echo/connect?transport=" + transport + "&connectionToken=" + connectionToken);
            }

            [Fact]
            public void SendToGroupFromOutsideOfConnection()
            {
                using (var host = new MemoryHost())
                {
                    IPersistentConnectionContext connectionContext = null;
                    host.Configure(app =>
                    {
                        var configuration = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapConnection<FilteredConnection>("/echo", configuration);
                        connectionContext = configuration.Resolver.Resolve<IConnectionManager>().GetConnectionContext<FilteredConnection>();
                    });

                    var connection1 = new Client.Connection("http://foo/echo");

                    using (connection1)
                    {
                        var wh1 = new ManualResetEventSlim(initialState: false);

                        connection1.Start(host).Wait();

                        connection1.Received += data =>
                        {
                            Assert.Equal("yay", data);
                            wh1.Set();
                        };

                        connectionContext.Groups.Add(connection1.ConnectionId, "Foo").Wait();
                        connectionContext.Groups.Send("Foo", "yay");

                        Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));
                    }
                }
            }

            [Theory]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            [InlineData(HostType.HttpListener, TransportType.Websockets)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling)]
            public void BasicAuthCredentialsFlow(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = CreateConnection(host, "/basicauth/echo");
                    var tcs = new TaskCompletionSource<string>();

                    using (connection)
                    {
                        connection.Credentials = new System.Net.NetworkCredential("user", "password");

                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                        };

                        connection.Start(host.Transport).Wait();

                        connection.SendWithTimeout("Hello World");

                        Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                        Assert.Equal("Hello World", tcs.Task.Result);
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.Auto, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Auto, MessageBusType.Default)]
            public void UnableToConnectToProtectedConnection(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var wh = new ManualResetEventSlim();
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/protected");

                    using (connection)
                    {
                        Assert.Throws<AggregateException>(() => connection.Start(host.Transport).Wait());
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            // [InlineData(HostType.Memory, TransportType.LongPolling)]
            // [InlineData(HostType.IISExpress, TransportType.Auto)]
            public void GroupCanBeAddedAndMessagedOnConnected(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var wh = new ManualResetEventSlim();
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/add-group");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            Assert.Equal("hey", data);
                            wh.Set();
                        };

                        connection.Start(host.Transport).Wait();
                        connection.SendWithTimeout("");

                        Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public void SendRaisesOnReceivedFromAllEvents(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/multisend");
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
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public void SendCanBeCalledAfterStateChangedEvent(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/multisend");
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
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            // [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void ReconnectFiresAfterHostShutDown(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/my-reconnect");

                    using (connection)
                    {
                        connection.Start(host.Transport).Wait();

                        host.Shutdown();

                        Thread.Sleep(TimeSpan.FromSeconds(5));

                        Assert.Equal(Client.ConnectionState.Reconnecting, connection.State);
                    }
                }
            }

            [Theory]
            [InlineData(TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            public void ReconnectFiresAfterTimeOut(TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = new MemoryHost())
                {
                    var conn = new MyReconnect();
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        UseMessageBus(messageBusType, config.Resolver);

                        app.MapConnection<MyReconnect>("/endpoint", config);
                        var configuration = config.Resolver.Resolve<IConfigurationManager>();
                        configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                        configuration.ConnectionTimeout = TimeSpan.FromSeconds(2);
                        configuration.KeepAlive = null;

                        config.Resolver.Register(typeof(MyReconnect), () => conn);
                    });

                    var connection = new Client.Connection("http://foo/endpoint");
                    var transport = CreateTransport(transportType, host);
                    connection.Start(transport).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Assert.InRange(conn.Reconnects, 1, 4);
                }
            }
        }

        public class GroupTest : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            public void GroupsReceiveMessages(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/groups");
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

                    for (int i = 0; i < 10; i++)
                    {
                        // Send a message
                        connection.SendWithTimeout(new { type = 3, group = "test", message = "goodbye to group test" });
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    connection.Stop();

                    Assert.Equal(1, list.Count);
                    Assert.Equal("hello to group test", list[0]);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            // [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public void GroupsRejoinedWhenOnRejoiningGroupsOverridden(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: null,
                                    disconnectTimeout: 6,
                                    connectionTimeout: 2,
                                    messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/rejoin-groups");

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
        }

        public class SendFacts : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public void SendToAllButCaller(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection1 = CreateConnection(host, "/filter");
                    var connection2 = CreateConnection(host, "/filter");

                    using (connection1)
                    using (connection2)
                    {
                        var wh1 = new ManualResetEventSlim(initialState: false);
                        var wh2 = new ManualResetEventSlim(initialState: false);

                        connection1.Received += data => wh1.Set();
                        connection2.Received += data => wh2.Set();

                        connection1.Start(host.TransportFactory()).Wait();
                        connection2.Start(host.TransportFactory()).Wait();

                        connection1.SendWithTimeout("test");

                        Assert.False(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                        Assert.True(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public void SendWithSyncErrorThrows(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/sync-error");

                    using (connection)
                    {
                        connection.Start(host.Transport).Wait();

                        Assert.Throws<AggregateException>(() => connection.SendWithTimeout("test"));
                    }
                }
            }
        }

        public class ReceiveFacts : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            public void ReceivePreserializedJson(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/preserialize");
                    var tcs = new TaskCompletionSource<string>();

                    connection.Received += json =>
                    {
                        tcs.TrySetResult(json);
                    };

                    using (connection)
                    {
                        connection.Start(host.Transport).Wait();

                        connection.SendWithTimeout(new { preserialized = true });

                        Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)));
                        var json = JObject.Parse(tcs.Task.Result);
                        Assert.True((bool)json["preserialized"]);
                    }
                }
            }
        }

        public class Owin : HostedTest
        {
            [Theory]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling)]
            public void EnvironmentIsAvailable(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = CreateConnection(host, "/items");
                    var connection2 = CreateConnection(host, "/items");

                    var results = new List<RequestItemsResponse>();
                    connection2.Received += data =>
                    {
                        var val = JsonConvert.DeserializeObject<RequestItemsResponse>(data);
                        if (!results.Contains(val))
                        {
                            results.Add(val);
                        }
                    };

                    connection.Start(host.TransportFactory()).Wait();
                    connection2.Start(host.TransportFactory()).Wait();

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    connection.SendWithTimeout(null);

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    connection.Stop();

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    Debug.WriteLine(String.Join(", ", results));

                    Assert.Equal(3, results.Count);
                    Assert.Equal("OnConnectedAsync", results[0].Method);
                    Assert.True(results[0].OwinKeys.Length > 0);
                    Assert.Equal("OnReceivedAsync", results[1].Method);
                    Assert.True(results[1].OwinKeys.Length > 0);
                    Assert.Equal("OnDisconnectAsync", results[2].Method);
                    Assert.True(results[1].OwinKeys.Length > 0);

                    connection2.Stop();
                }
            }
        }
    }
}
