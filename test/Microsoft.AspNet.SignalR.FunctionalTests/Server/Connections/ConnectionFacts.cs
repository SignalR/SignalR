// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.Owin;
using Owin;
using Xunit;

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
                            messageTcs.TrySetResult(message);
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

                        Assert.Equal("testMessage", await messageTcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            //[InlineData(HostType.Memory, TransportType.Auto, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.Auto, MessageBusType.Default)]
            public async Task ThrownWebExceptionShouldBeUnwrapped(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(messageBusType: messageBusType, transportConnectTimeout: 30);

                    var connection = CreateConnection(host, "/ErrorsAreFun");

                    // Expecting 404
                    var aggEx = await Assert.ThrowsAsync<StartException>(() => connection.Start(host.Transport));

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
                    var connection = new Connection("http://foo/echo2");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        var dataReceived = await tcs.Task.OrTimeout();
                        Assert.Equal("MyConnection2", dataReceived);
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
                    var connection = new Connection("http://foo/echo2");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        Assert.Equal("MyConnection2", await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));
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
                    var connection = new Connection("http://foo/echo");

                    using (connection)
                    {
                        connection.Received += data =>
                        {
                            tcs.TrySetResult(data);
                        };

                        await connection.Start(host);
                        var ignore = connection.Send("");

                        Assert.Equal("MyConnection", await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));
                    }
                }
            }

            [Theory]
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

                    connection.Closed += () =>
                    {
                        tcs.TrySetResult(null);
                    };

                    await connection.Start(host.Transport);

                    connection.Stop();

                    await tcs.Task.OrTimeout();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
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
            //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
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
            //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
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
                        return;
                    }

                    Assert.True(false, "An exception should have been thrown.");
                }
            }


            [Theory]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            private async Task ClientStopsReconnectingAfterDisconnectTimeout(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(disconnectTimeout: 6, messageBusType: messageBusType);
                    var connection = CreateHubConnection(host);

                    using (connection)
                    {
                        var reconnectWh = new TaskCompletionSource<object>();
                        var disconnectWh = new TaskCompletionSource<object>();

                        connection.Reconnecting += () =>
                        {
                            reconnectWh.TrySetResult(null);
                            Assert.Equal(ConnectionState.Reconnecting, connection.State);
                        };

                        connection.Closed += () =>
                        {
                            disconnectWh.TrySetResult(null);
                            Assert.Equal(ConnectionState.Disconnected, connection.State);
                        };

                        await connection.Start(host.Transport);
                        host.Shutdown();

                        await reconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(25));
                        await disconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(25));
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
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
                        var reconnectingWh = new TaskCompletionSource<object>();
                        var reconnectedWh = new TaskCompletionSource<object>();

                        connection.Reconnecting += () =>
                        {
                            reconnectingWh.TrySetResult(null);
                            Assert.Equal(ConnectionState.Reconnecting, connection.State);
                        };

                        connection.Reconnected += () =>
                        {
                            reconnectedWh.TrySetResult(null);
                            Assert.Equal(ConnectionState.Connected, connection.State);
                        };

                        await connection.Start(host.Transport);

                        // Force reconnect
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        await reconnectingWh.Task.OrTimeout(TimeSpan.FromSeconds(30));
                        await reconnectedWh.Task.OrTimeout(TimeSpan.FromSeconds(30));

                        await Task.Delay(TimeSpan.FromSeconds(8));
                        Assert.NotEqual(ConnectionState.Disconnected, connection.State);
                    }
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
            [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
            [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
            public async Task ConnectionErrorCapturesExceptionsThrownInReceived(HostType hostType, TransportType transportType, MessageBusType messageBusType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var errorsCaught = 0;
                    var wh = new TaskCompletionSource<object>();
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
                                wh.TrySetResult(null);
                            }
                        };

                        await connection.Start(host.Transport);

                        await wh.Task.OrTimeout(TimeSpan.FromSeconds(5));
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

                        var wh = new TaskCompletionSource<object>();
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
                                wh.TrySetResult(null);
                            };

                            await connection.Start(transport);
                            var ignore = connection.Send("");

                            await wh.Task.OrTimeout();
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
