using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting.Memory;
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
            public void ConnectionCanStartWithAuthenicatedUserAndQueryString()
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
                        app.MapConnection<MyAuthenticatedConnection>("/authenticatedConnection", new ConnectionConfiguration());

                    });

                    var connection = new Connection("http://foo/authenticatedConnection", "access_token=1234");

                    connection.Start(host).Wait();

                    Assert.Equal(connection.State, ConnectionState.Connected);

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IISExpress, TransportType.LongPolling)] // Connect has issues with LP
            public void ThrownWebExceptionShouldBeUnwrapped(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = CreateConnection(host, "/ErrorsAreFun");

                    // Expecting 404
                    var aggEx = Assert.Throws<AggregateException>(() => connection.Start(host.Transport).Wait());

                    connection.Stop();

                    using (var ser = aggEx.GetError())
                    {
                        if (hostType == HostType.IISExpress)
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
            public void FallbackToLongPollingIIS()
            {
                using (ITestHost host = CreateHost(HostType.IISExpress))
                {
                    host.Initialize();

                    var connection = CreateConnection(host, "/fall-back");
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
                        new ServerSentEventsTransport(client) { ConnectionTimeout = TimeSpan.Zero },
                        new LongPollingTransport(client)
                    };

                    var transport = new AutoTransport(client, transports);

                    connection.Start(transport).Wait();

                    Assert.Equal(connection.Transport.Name, "longPolling");

                    Assert.False(tcs.Task.Wait(TimeSpan.FromSeconds(5)));

                    connection.Stop();
                }
            }

            [Fact]
            public void PrefixMatchingIsNotGreedy()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        app.MapConnection<MyConnection>("/echo");
                        app.MapConnection<MyConnection2>("/echo2");
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var connection = new Connection("http://foo/echo2");

                    connection.Received += data =>
                    {
                        tcs.TrySetResult(data);
                    };

                    connection.Start(host).Wait();
                    connection.Send("");

                    Assert.Equal("MyConnection2", tcs.Task.Result);
                }
            }

            [Fact]
            public void PrefixMatchingIsNotGreedyNotStartingWithSlashes()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        app.MapConnection<MyConnection>("echo");
                        app.MapConnection<MyConnection2>("echo2");
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var connection = new Connection("http://foo/echo2");

                    connection.Received += data =>
                    {
                        tcs.TrySetResult(data);
                    };

                    connection.Start(host).Wait();
                    connection.Send("");

                    Assert.Equal("MyConnection2", tcs.Task.Result);
                }
            }

            [Fact]
            public void PrefixMatchingIsNotGreedyExactMatch()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        app.MapConnection<MyConnection>("echo");
                        app.MapConnection<MyConnection2>("echo2");
                    });

                    var tcs = new TaskCompletionSource<string>();
                    var connection = new Connection("http://foo/echo");

                    connection.Received += data =>
                    {
                        tcs.TrySetResult(data);
                    };

                    connection.Start(host).Wait();
                    connection.Send("");

                    Assert.Equal("MyConnection", tcs.Task.Result);
                }
            }

            [Theory]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void StoppingDoesntRaiseErrorEvent(HostType hostType, TransportType transportType)
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

                    connection.Start(host.Transport).Wait();

                    connection.Stop();

                    tcs.Task.Wait(TimeSpan.FromSeconds(5));
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void ManuallyRestartedClientMaintainsConsistentState(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();
                    var connection = CreateHubConnection(host);
                    int timesStopped = 0;

                    connection.Closed += () =>
                    {
                        timesStopped++;
                        Assert.Equal(ConnectionState.Disconnected, connection.State);
                    };

                    for (int i = 0; i < 5; i++)
                    {
                        connection.Start(host.TransportFactory()).Wait();
                        connection.Stop();
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        connection.Start(host.TransportFactory());
                        connection.Stop();
                    }
                    Assert.Equal(15, timesStopped);
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void ClientStopsReconnectingAfterDisconnectTimeout(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(disconnectTimeout: 6);
                    var connection = CreateHubConnection(host);
                    var reconnectWh = new ManualResetEventSlim();
                    var disconnectWh = new ManualResetEventSlim();

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

                    connection.Start(host.Transport).Wait();
                    host.Shutdown();

                    Assert.True(reconnectWh.Wait(TimeSpan.FromSeconds(25)), "Reconnect never fired");
                    Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(25)), "Closed never fired");
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            [InlineData(HostType.IISExpress, TransportType.LongPolling)]
            public void ClientStaysReconnectedAfterDisconnectTimeout(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize(keepAlive: null,
                                    connectionTimeout: 2,
                                    disconnectTimeout: 6);

                    var connection = CreateHubConnection(host);
                    var reconnectingWh = new ManualResetEventSlim();
                    var reconnectedWh = new ManualResetEventSlim();

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

                    connection.Start(host.Transport).Wait();

                    // Force reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    Assert.True(reconnectingWh.Wait(TimeSpan.FromSeconds(30)));
                    Assert.True(reconnectedWh.Wait(TimeSpan.FromSeconds(30)));
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    Assert.NotEqual(ConnectionState.Disconnected, connection.State);

                    connection.Stop();
                }
            }

            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.Websockets)]
            public void ConnectionErrorCapturesExceptionsThrownInReceived(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    var errorsCaught = 0;
                    var wh = new ManualResetEventSlim();
                    Exception thrown = new Exception(),
                              caught = null;

                    host.Initialize();

                    var connection = CreateConnection(host, "/multisend");

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

                    connection.Start(host.Transport).Wait();

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
                    Assert.Equal(thrown, caught);
                }
            }

            [Fact]
            public void ConnectionErrorCapturesExceptionsThrownWhenReceivingResponseFromSend()
            {
                using (var host = new MemoryHost())
                {
                    host.Configure(app =>
                    {
                        app.MapConnection<TransportResponse>("/transport-response");
                    });

                    var transports = new List<IClientTransport>()
                    {
                        new ServerSentEventsTransport(host),
                        new LongPollingTransport(host)
                    };

                    foreach (var transport in transports)
                    {
                        Debug.WriteLine("Transport: {0}", (object)transport.Name);

                        var wh = new ManualResetEventSlim();
                        Exception thrown = new Exception(),
                                  caught = null;

                        var connection = new Connection("http://foo/transport-response");

                        connection.Received += data =>
                        {
                            throw thrown;
                        };

                        connection.Error += e =>
                        {
                            caught = e;
                            wh.Set();
                        };

                        connection.Start(transport).Wait();
                        connection.Send("");

                        Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
                        Assert.IsType(typeof(AggregateException), caught);
                        Assert.Equal(thrown, caught.InnerException);
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
