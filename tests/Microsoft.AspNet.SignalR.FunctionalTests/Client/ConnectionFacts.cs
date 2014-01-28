using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ConnectionFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public void MarkActiveStopsConnectionIfCalledAfterExtendedPeriod(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var disconnectWh = new ManualResetEventSlim();

                    connection.Closed += () =>
                    {
                        disconnectWh.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    // The MarkActive interval should check the reconnect window. Since this is short it should force the connection to disconnect.
                    ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromSeconds(1);

                    Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(15)), "Closed never fired");
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
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public void ReconnectExceedingReconnectWindowDisconnectsWithFastBeatInterval(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: 9, messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var disconnectWh = new ManualResetEventSlim();

                    connection.Closed += () =>
                    {
                        disconnectWh.Set();
                    };

                    SetReconnectDelay(host.Transport, TimeSpan.FromSeconds(15));

                    connection.Start(host.Transport).Wait();                    

                    // Without this the connection start and reconnect can race with eachother resulting in a deadlock.
                    Thread.Sleep(TimeSpan.FromSeconds(3));

                    // Set reconnect window to zero so the second we attempt to reconnect we can ensure that the reconnect window is verified.
                    ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromSeconds(0);

                    host.Shutdown();

                    Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(15)), "Closed never fired");
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
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public void ReconnectExceedingReconnectWindowDisconnects(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var reconnectWh = new ManualResetEventSlim();
                    var disconnectWh = new ManualResetEventSlim();

                    connection.Reconnecting += () =>
                    {
                        ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromMilliseconds(500);
                        reconnectWh.Set();
                    };

                    connection.Closed += () =>
                    {
                        disconnectWh.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    // Without this the connection start and reconnect can race with eachother resulting in a deadlock.
                    Thread.Sleep(TimeSpan.FromSeconds(3));

                    host.Shutdown();

                    Assert.True(reconnectWh.Wait(TimeSpan.FromSeconds(15)), "Reconnect never fired");
                    Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(15)), "Closed never fired");
                }
            }
        }

        [Fact]
        public void NoReconnectsAfterFallback()
        {
            // There was a regression where the SSE transport would try to reconnect after it times out.
            // This test ensures that no longer happens.
            // #2180
            using (var host = new MemoryHost())
            {
                var myReconnect = new MyReconnect();

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

                    config.Resolver.Register(typeof(MyReconnect), () => myReconnect);

                    app.MapSignalR<MyReconnect>("/echo", config);
                });

                var connection = new Connection("http://foo/echo");

                using (connection)
                {
                    connection.Start(host).Wait();

                    // Give SSE an opportunity to reconnect
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    Assert.Equal(connection.State, ConnectionState.Connected);
                    Assert.Equal(connection.Transport.Name, "longPolling");
                    Assert.Equal(0, myReconnect.Reconnects);
                }
            }
        }

        [Fact]
        public void WebSocketsTransportFailsIfOnConnectedThrows()
        {
            using (ITestHost host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                var connection = CreateConnection(host, "/fall-back-throws");

                using (connection)
                {
                    Assert.Throws<AggregateException>(() => connection.Start(new WebSocketTransport()).Wait());
                }
            }
        }

        [Fact]
        public void TransportConnectTimeoutDoesNotAddupOverNegotiateRequests()
        {
            using (ITestHost host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();
                var connection = CreateConnection(host, "/signalr");
                connection.TransportConnectTimeout = TimeSpan.FromSeconds(5);

                using (connection)
                {
                    connection.Start().Wait();
                    var totalTransportConnectTimeout = ((Client.IConnection)connection).TotalTransportConnectTimeout;
                    connection.Stop();
                    connection.Start().Wait();
                    Assert.Equal(((Client.IConnection)connection).TotalTransportConnectTimeout, totalTransportConnectTimeout);
                }
            }
        }

        [Theory]
        [InlineData("1337.0", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("1337.0", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData("1337.0", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData("1337.0", HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("1337.0", HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData("1337.0", HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData("1337.0", HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("1337.0", HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("1337.0", HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData("0.1337", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("0.1337", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData("0.1337", HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData("0.1337", HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("0.1337", HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData("0.1337", HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData("0.1337", HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData("0.1337", HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData("0.1337", HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public void ConnectionFailsToStartWithInvalidOldProtocol(string protocolVersion, HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateConnection(host, "/signalr");

                connection.Protocol = new Version(protocolVersion);

                using (connection)
                {
                    connection.Start(host.Transport).ContinueWith(task =>
                    {
                        Assert.True(task.IsFaulted);
                    }).Wait();
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
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public void ConnectionDisposeTriggersStop(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateConnection(host, "/signalr");

                using (connection)
                {
                    connection.Start(host.Transport).Wait();
                    Assert.Equal(connection.State, Client.ConnectionState.Connected);
                }

                Assert.Equal(connection.State, Client.ConnectionState.Disconnected);
            }
        }


        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void RequestHeadersSetCorrectly(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var tcs = new TaskCompletionSource<object>();
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");

                using (connection)
                {
                    connection.Received += arg =>
                    {
                        JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                        if (transportType != TransportType.Websockets)
                        {
                            Assert.Equal("referer", (string)headers["refererHeader"]);
                            Assert.NotNull((string)headers["userAgentHeader"]);
                        }

                        Assert.Equal("test-header", (string)headers["testHeader"]);

                        tcs.TrySetResult(null);
                    };

                    connection.Error += e => tcs.TrySetException(e);

                    connection.Headers.Add("test-header", "test-header");

                    if (transportType != TransportType.Websockets)
                    {
                        connection.Headers.Add(System.Net.HttpRequestHeader.Referer.ToString(), "referer");
                    }

                    connection.Start(host.Transport).Wait();
                    connection.Send("Hello");

                    // Assert
                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        public void RequestHeadersCanBeSetOnceConnected(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");
                var mre = new ManualResetEventSlim();

                using (connection)
                {
                    connection.Received += arg =>
                    {
                        JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                        Assert.Equal("test-header", (string)headers["testHeader"]);

                        mre.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    connection.Headers.Add("test-header", "test-header");
                    connection.Send("message");

                    // Assert
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));
                }

            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void ReconnectRequestPathEndsInReconnect(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var tcs = new TaskCompletionSource<bool>();
                var receivedMessage = false;

                host.Initialize(keepAlive: null,
                                connectionTimeout: 2,
                                disconnectTimeout: 6);

                var connection = CreateConnection(host, "/force-lp-reconnect/examine-reconnect");

                using (connection)
                {
                    connection.Received += (reconnectEndsPath) =>
                    {
                        if (!receivedMessage)
                        {
                            tcs.TrySetResult(reconnectEndsPath == "True");
                            receivedMessage = true;
                        }
                    };

                    connection.Start(host.Transport).Wait();

                    // Wait for reconnect
                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                    Assert.True(tcs.Task.Result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public void ConnectionFunctionsCorrectlyAfterCallingStartMutlipleTimes(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateConnection(host, "/autoencodedjson"))
                {
                    var tcs = new TaskCompletionSource<object>();
                    connection.Received += _ => tcs.TrySetResult(null);

                    // We're purposely calling Start().Wait() twice here
                    connection.Start(host.TransportFactory()).Wait();
                    connection.Start(host.TransportFactory()).Wait();

                    connection.Send("test").Wait();

                    // Wait for message to be received
                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }
    }
}
