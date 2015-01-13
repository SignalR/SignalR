using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.Owin;
using Owin;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ConnectionFacts
    {
        public class Start : HostedTest
        {
            [Fact]
            public async Task InitMessageSentToFallbackTransports()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        Func<AppFunc, AppFunc> middleware = (next) =>
                        {
                            return env =>
                            {
                                var request = new OwinRequest(env);
                                var response = new OwinResponse(env);

                                if (!request.Path.Value.Contains("negotiate") && !request.QueryString.Value.Contains("longPolling"))
                                {
                                    response.Body = new MemoryStream();
                                }

                                return next(env);
                            };
                        };

                        app.Use(middleware);

                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<MyConnection>("/echo", config);
                    });

                    var connection = new Connection("http://foo/echo");

                    using (connection)
                    {
                        await connection.Start(host);

                        Assert.Equal(connection.State, ConnectionState.Connected);
                        Assert.Equal(connection.Transport.Name, "longPolling");
                    }
                }
            }

            [Fact]
            public async Task ConnectionCanStartWithAuthenicatedUserAndQueryString()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        Func<AppFunc, AppFunc> middleware = (next) =>
                        {
                            return env =>
                            {
                                if (((string)env["owin.RequestQueryString"]).IndexOf("access_token") == -1)
                                {
                                    return next(env);
                                }

                                var user = new CustomPrincipal
                                {
                                    Name = "Bob",
                                    IsAuthenticated = true,
                                    Roles = new[] { "User" }
                                };

                                env["server.User"] = user;

                                return next(env);
                            };
                        };

                        app.Use(middleware);

                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<MyAuthenticatedConnection>("/authenticatedConnection", config);

                    });

                    var connection = new Connection("http://foo/authenticatedConnection", "access_token=1234");

                    using (connection)
                    {
                        await connection.Start(host);

                        Assert.Equal(connection.State, ConnectionState.Connected);
                    }
                }
            }

            [Fact]
            public async Task ConnectionCanAddAnotherConnectionOnAnotherHostToAGroup()
            {
                using (var host1 = new MemoryHost())
                using (var host2 = new MemoryHost())
                {
                    var sharedBus = new DefaultDependencyResolver().Resolve<IMessageBus>();
                    host1.Configure(app =>
                    {
                        var resolver = new DefaultDependencyResolver();
                        var ackHandler = new SignalR.Infrastructure.AckHandler(
                            completeAcksOnTimeout: true,
                            ackThreshold: TimeSpan.FromSeconds(10),
                            ackInterval: TimeSpan.FromSeconds(1));

                        resolver.Register(typeof(SignalR.Infrastructure.IAckHandler), () => ackHandler);
                        resolver.Register(typeof(IMessageBus), () => sharedBus);

                        app.MapSignalR<MyGroupConnection>("/groups", new ConnectionConfiguration
                        {
                            Resolver = resolver
                        });
                    });
                    host2.Configure(app =>
                    {
                        var resolver = new DefaultDependencyResolver();

                        resolver.Register(typeof(IMessageBus), () => sharedBus);

                        app.MapSignalR<MyGroupConnection>("/groups", new ConnectionConfiguration
                        {
                            Resolver = resolver
                        });
                    });

                    using (var connection1 = new Connection("http://foo/groups"))
                    using (var connection2 = new Connection("http://foo/groups"))
                    {
                        var messageTcs = new TaskCompletionSource<string>();

                        connection2.Received += message =>
                        {
                            messageTcs.SetResult(message);
                        };

                        await connection1.Start(host1);
                        await connection2.Start(host2);

                        await connection1.Send(new
                        {
                            // Add to group
                            type = 1,
                            group = "testGroup",
                            connectionId = connection2.ConnectionId
                        });

                        await connection1.Send(new
                        {
                            // Send to group
                            type = 3,
                            group = "testGroup",
                            message = "testMessage"
                        });

                        Assert.True(messageTcs.Task.Wait(TimeSpan.FromSeconds(10)));
                        Assert.Equal("testMessage", messageTcs.Task.Result);
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
            [InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.Auto, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Auto, MessageBusType.Default)]
            public void ThrownWebExceptionShouldBeUnwrapped(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType, transportConnectTimeout: 30);

                    var connection = CreateConnection(host, "/ErrorsAreFun");

                    // Expecting 404
                    var aggEx = Assert.Throws<AggregateException>(() => connection.Start(host.Transport).Wait());

                    connection.Stop();

                    using (var ser = aggEx.GetError())
                    {
                        if (hostType == HostType.IISExpress || hostType == HostType.HttpListener)
                        {
                            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, ser.StatusCode);
                        }
                        else
                        {
                            Assert.Equal(System.Net.HttpStatusCode.NotFound, ser.StatusCode);
                        }

                        Assert.NotNull(ser.ResponseBody);
                        Assert.NotNull(ser.Exception);
                    }
                }
            }

            [Fact]
            public async Task ConnectionUsesClientSetTransportConnectTimeout()
            {
                TimeSpan defaultTransportConnectTimeout = TimeSpan.Zero;
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        defaultTransportConnectTimeout = config.Resolver.Resolve<IConfigurationManager>().TransportConnectTimeout;

                        app.MapSignalR<MyConnection>("/echo", config);
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var connection = new Connection("http://foo/echo");
                    var newTimeout = TimeSpan.FromSeconds(4);

                    using (connection)
                    {
                        Assert.Equal(((IConnection)connection).TotalTransportConnectTimeout, TimeSpan.Zero);
                        connection.TransportConnectTimeout = newTimeout;
                        await connection.Start(host);
                        Assert.Equal(((IConnection)connection).TotalTransportConnectTimeout - defaultTransportConnectTimeout, newTimeout);
                    }
                }
            }

            [Fact]
            public async Task FallbackToLongPollingIIS()
            {
                using (ITestHost host = CreateHost(HostType.IISExpress))
                {
                    // Reduce transportConnectionTimeout to 5 seconds
                    host.Initialize(transportConnectTimeout: 5);

                    var connection = CreateConnection(host, "/fall-back");

                    using (connection)
                    {
                        var tcs = new TaskCompletionSource<object>();

                        connection.StateChanged += change =>
                        {
                            if (change.NewState == ConnectionState.Reconnecting)
                            {
                                tcs.TrySetException(new Exception("The connection should not be reconnecting"));
                            }
                        };

                        var client = new DefaultHttpClient();
                        var transports = new IClientTransport[]  {
                            new ServerSentEventsTransport(client),
                            new LongPollingTransport(client)
                        };

                        var transport = new AutoTransport(client, transports);

                        await connection.Start(transport);

                        Assert.Equal(connection.Transport.Name, "longPolling");

                        Assert.False(tcs.Task.Wait(TimeSpan.FromSeconds(5)));
                    }
                }
            }

            [Fact]
            public void ConnectionCanBeEstablishedWithPreSendRequestHeadersEventAttached()
            {
                using (ITestHost host = CreateHost(HostType.IISExpress))
                {
                    ((IISExpressTestHost)host).AttachToPreSendRequestHeaders = true;
                    host.Initialize();

                    var connection = CreateConnection(host, "/async-on-connected");

                    using (connection)
                    {
                        Assert.True(connection.Start().Wait(TimeSpan.FromSeconds(10)), "The connection failed to start.");
                    }
                }
            }

            [Fact]
            public async Task PrefixMatchingIsNotGreedy()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<MyConnection>("/echo", config);
                        app.MapSignalR<MyConnection2>("/echo2", config);
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var mre = new AsyncManualResetEvent();
                    var connection = new Connection("http://foo/echo2");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                            mre.Set();
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(5)));
                        Assert.Equal("MyConnection2", tcs.Task.Result);
                    }
                }
            }

            [Fact]
            public async Task PrefixMatchingIsNotGreedyNotStartingWithSlashes()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<MyConnection>("/echo", config);
                        app.MapSignalR<MyConnection2>("/echo2", config);
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var mre = new AsyncManualResetEvent();
                    var connection = new Connection("http://foo/echo2");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                            mre.Set();
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(10)));
                        Assert.Equal("MyConnection2", tcs.Task.Result);
                    }
                }
            }

            [Fact]
            public async Task PrefixMatchingIsNotGreedyExactMatch()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<MyConnection>("/echo", config);
                        app.MapSignalR<MyConnection2>("/echo2", config);
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var mre = new AsyncManualResetEvent();
                    var connection = new Connection("http://foo/echo");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                            mre.Set();
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(10)));
                        Assert.Equal("MyConnection", tcs.Task.Result);
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
            public async Task StoppingDoesntRaiseErrorEvent(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();
                    var connection = CreateHubConnection(host);

                    var tcs = new TaskCompletionSource<object>();
                    connection.Error += ex =>
                    {
                        tcs.TrySetException(ex);
                    };

                    await connection.Start(host.Transport);

                    connection.Stop();

                    tcs.Task.Wait(TimeSpan.FromSeconds(5));
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
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public async Task ManuallyRestartedClientMaintainsConsistentState(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);
                    var connection = CreateHubConnection(host);
                    int timesStopped = 0;

                    connection.Closed += () =>
                    {
                        timesStopped++;
                        Assert.Equal(ConnectionState.Disconnected, connection.State);
                    };

                    for (int i = 0; i < 5; i++)
                    {
                        await connection.Start(host.TransportFactory());
                        connection.Stop();
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        var ignore = connection.Start(host.TransportFactory());
                        connection.Stop();
                    }
                    Assert.Equal(15, timesStopped);
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
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public async Task AwaitingOnStartAndThenStoppingDoesntHang(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);
                    var connection = CreateHubConnection(host);

                    var startTime = DateTime.UtcNow;

                    await connection.Start(host.TransportFactory());
                    connection.Stop();

                    Assert.True(DateTime.UtcNow - startTime < TimeSpan.FromSeconds(10));
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
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public async Task AwaitingOnFailedStartAndThenStoppingDoesntHang(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType);
                    var badConnection = CreateConnection(host, "/ErrorsAreFun");

                    var startTime = DateTime.UtcNow;

                    try
                    {
                        await badConnection.Start(HostedTestFactory.CreateTransport(transportType));
                    }
                    catch
                    {
                        badConnection.Stop();
                        Assert.True(DateTime.UtcNow - startTime < TimeSpan.FromSeconds(10));
                        return;
                    }

                    Assert.True(false, "An exception should have been thrown.");
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
            public async Task ClientStopsReconnectingAfterDisconnectTimeout(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(disconnectTimeout: 6, messageBusType: messageBusType);
                    var connection = CreateHubConnection(host);

                    using (connection)
                    {
                        var reconnectWh = new AsyncManualResetEvent();
                        var disconnectWh = new AsyncManualResetEvent();

                        connection.Reconnecting += () =>
                        {
                            reconnectWh.Set();
                            Assert.Equal(ConnectionState.Reconnecting, connection.State);
                        };

                        connection.Closed += () =>
                        {
                            disconnectWh.Set();
                            Assert.Equal(ConnectionState.Disconnected, connection.State);
                        };

                        await connection.Start(host.Transport);
                        host.Shutdown();

                        Assert.True(await reconnectWh.WaitAsync(TimeSpan.FromSeconds(25)), "Reconnect never fired");
                        Assert.True(await disconnectWh.WaitAsync(TimeSpan.FromSeconds(25)), "Closed never fired");
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
            [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            public async Task ClientStaysReconnectedAfterDisconnectTimeout(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: null,
                                    connectionTimeout: 2,
                                    disconnectTimeout: 8, // 8s because the default heartbeat time span is 5s
                                    messageBusType: messageBusType);

                    using (var connection = CreateHubConnection(host, "/force-lp-reconnect"))
                    {
                        var reconnectingWh = new AsyncManualResetEvent();
                        var reconnectedWh = new AsyncManualResetEvent();

                        connection.Reconnecting += () =>
                        {
                            reconnectingWh.Set();
                            Assert.Equal(ConnectionState.Reconnecting, connection.State);
                        };

                        connection.Reconnected += () =>
                        {
                            reconnectedWh.Set();
                            Assert.Equal(ConnectionState.Connected, connection.State);
                        };

                        await connection.Start(host.Transport);

                        // Force reconnect
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        Assert.True(await reconnectingWh.WaitAsync(TimeSpan.FromSeconds(30)));
                        Assert.True(await reconnectedWh.WaitAsync(TimeSpan.FromSeconds(30)));

                        await Task.Delay(TimeSpan.FromSeconds(15));
                        Assert.NotEqual(ConnectionState.Disconnected, connection.State);
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
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            public async Task ConnectionErrorCapturesExceptionsThrownInReceived(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var errorsCaught = 0;
                    var wh = new AsyncManualResetEvent();
                    Exception thrown = new Exception(),
                              caught = null;

                    host.Initialize(messageBusType: messageBusType);

                    var connection = CreateConnection(host, "/multisend");

                    using (connection)
                    {
                        connection.Received += _ =>
                        {
                            throw thrown;
                        };

                        connection.Error += e =>
                        {
                            caught = e;
                            if (Interlocked.Increment(ref errorsCaught) == 2)
                            {
                                wh.Set();
                            }
                        };

                        await connection.Start(host.Transport);

                        Assert.True(await wh.WaitAsync(TimeSpan.FromSeconds(5)));
                        Assert.Equal(thrown, caught);
                    }
                }
            }

            [Fact]
            public async Task ConnectionErrorCapturesExceptionsThrownWhenReceivingResponseFromSend()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        var config = new ConnectionConfiguration
                        {
                            Resolver = new DefaultDependencyResolver()
                        };

                        app.MapSignalR<TransportResponse>("/transport-response", config);
                    });

                    var transports = new List<IClientTransport>()
                    {
                        new ServerSentEventsTransport(host),
                        new LongPollingTransport(host)
                    };

                    foreach (var transport in transports)
                    {
                        Debug.WriteLine("Transport: {0}", (object)transport.Name);

                        var wh = new AsyncManualResetEvent();
                        Exception thrown = new InvalidOperationException(),
                                  caught = null;

                        var connection = new Connection("http://foo/transport-response");

                        using (connection)
                        {
                            connection.Received += data =>
                            {
                                throw thrown;
                            };

                            connection.Error += e =>
                            {
                                caught = e;
                                wh.Set();
                            };

                            await connection.Start(transport);
                            var ignore = connection.Send("");

                            Assert.True(await wh.WaitAsync(TimeSpan.FromSeconds(5)));
                            Assert.Equal(thrown, caught);
                        }
                    }
                }
            }
        }

        private class MyConnection : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Send(connectionId, "MyConnection");
            }
        }

        private class MyConnection2 : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Send(connectionId, "MyConnection2");
            }
        }

        private class TransportResponse : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Transport.Send(new object());
            }
        }

        private class CustomPrincipal : IIdentity, IPrincipal
        {
            public string AuthenticationType
            {
                get { return "Forms"; }
            }

            public bool IsAuthenticated { get; set; }

            public string Name { get; set; }
            public string[] Roles { get; set; }

            public IIdentity Identity
            {
                get { return this; }
            }

            public bool IsInRole(string role)
            {
                return Roles != null && Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
            }
        }

        public class MyAuthenticatedConnection : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Task.Run(() =>
                {
                    GlobalHost.ConnectionManager.GetConnectionContext<MyAuthenticatedConnection>()
                        .Connection.Send(connectionId, data);
                });
            }
        }
    }
}
