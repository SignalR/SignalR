using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class ConnectionFacts
    {
        public class Start : HostedTest
        {
            [Theory]
            [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
            [InlineData(HostType.Memory, TransportType.LongPolling)]
            [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
            // [InlineData(HostType.IIS, TransportType.LongPolling)]
            public void ThrownWebExceptionShouldBeUnwrapped(HostType hostType, TransportType transportType)
            {
                using (var host = CreateHost(hostType, transportType))
                {
                    host.Initialize();

                    var connection = CreateConnection(host.Url + "/ErrorsAreFun");

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
                using (ITestHost host = new IISExpressTestHost())
                {
                    host.Initialize();

                    var connection = CreateConnection(host.Url + "/fall-back");
                    var tcs = new TaskCompletionSource<object>();

                    connection.StateChanged += change =>
                    {
                        if (change.NewState == ConnectionState.Reconnecting)
                        {
                            tcs.TrySetException(new Exception("The connection should not be reconnecting"));
                        }
                    };

                    var transports = new IClientTransport[]  {
                        new ServerSentEventsTransport(),
                        new LongPollingTransport()
                    };

                    var transport = new AutoTransport(new DefaultHttpClient(), transports);

                    connection.Start(transport).Wait();

                    Assert.Equal(connection.Transport.Name, "longPolling");

                    Assert.False(tcs.Task.Wait(TimeSpan.FromSeconds(10)));

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
                        connection.Start(host.Transport).Wait();
                        connection.Stop();
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        connection.Start(host.Transport);
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

                    Assert.True(reconnectWh.Wait(TimeSpan.FromSeconds(15)));
                    Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(15)));
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

                    var connection = new Connection(host.Url + "/multisend");

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
    }
}
