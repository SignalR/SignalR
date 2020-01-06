// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Connection = Client.Connection;

    public class ConnectionFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task MarkActiveStopsConnectionIfCalledAfterExtendedPeriod(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var disconnectWh = new TaskCompletionSource<object>();

                    connection.Closed += () =>
                    {
                        disconnectWh.TrySetResult(null);
                    };

                    await connection.Start(host.Transport).OrTimeout();

                    // The MarkActive interval should check the reconnect window. Since this is short it should force the connection to disconnect.
                    ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromSeconds(1);

                    await disconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(15));
                }
            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task ReconnectExceedingReconnectWindowDisconnectsWithFastBeatInterval(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: 9, messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var disconnectWh = new TaskCompletionSource<object>();

                    connection.Closed += () =>
                    {
                        disconnectWh.TrySetResult(null);
                    };

                    SetReconnectDelay(host.Transport, TimeSpan.FromSeconds(15));

                    await connection.Start(host.Transport).OrTimeout();

                    // Without this the connection start and reconnect can race with eachother resulting in a deadlock.
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    // Set reconnect window to zero so the second we attempt to reconnect we can ensure that the reconnect window is verified.
                    ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromSeconds(0);

                    host.Shutdown();

                    await disconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(15));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ReconnectExceedingReconnectWindowDisconnects(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var reconnectWh = new TaskCompletionSource<object>();
                    var disconnectWh = new TaskCompletionSource<object>();

                    connection.Reconnecting += () =>
                    {
                        ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromMilliseconds(500);
                        reconnectWh.TrySetResult(null);
                    };

                    connection.Closed += () =>
                    {
                        disconnectWh.TrySetResult(null);
                    };

                    await connection.Start(host.Transport);

                    // Without this the connection start and reconnect can race with eachother resulting in a deadlock.
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    host.Shutdown();

                    await reconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(15));
                    await disconnectWh.Task.OrTimeout(TimeSpan.FromSeconds(15));
                }
            }
        }

        [Fact]
        public async Task NoReconnectsAfterFallbackDueToTimeout()
        {
            // There was a regression where the SSE transport would try to reconnect after it timed out.
            // This test ensures that no longer happens.
            // #2180
            using (var host = new MemoryHost())
            {
                var reconnects = 0;

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

                    config.Resolver.Register(typeof(MyReconnect), () => new MyReconnect(() => reconnects++));
                    config.Resolver.Resolve<IConfigurationManager>().TransportConnectTimeout = TimeSpan.FromSeconds(1);

                    app.MapSignalR<MyReconnect>("/echo", config);
                });

                var connection = new Connection("http://foo/echo");

                using (connection)
                {
                    await connection.Start(host).OrTimeout();

                    // Give SSE an opportunity to reconnect
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    Assert.Equal(connection.State, ConnectionState.Connected);
                    Assert.Equal(connection.Transport.Name, "longPolling");
                    Assert.Equal(0, reconnects);
                }
            }
        }

        [Fact]
        public async Task NoReconnectsAfterFallbackDueToDisconnect()
        {
            using (var host = new MemoryHost())
            {
                var serverReconnects = 0;

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
                                return Task.CompletedTask;
                            }

                            return next(env);
                        };
                    };

                    app.Use(middleware);

                    var config = new ConnectionConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    config.Resolver.Register(typeof(MyReconnect), () => new MyReconnect(() => serverReconnects++));
                    config.Resolver.Resolve<IConfigurationManager>().TransportConnectTimeout = TimeSpan.FromSeconds(60);

                    app.MapSignalR<MyReconnect>("/echo", config);
                });

                var connection = new Connection("http://foo/echo");

                using (connection)
                {
                    var clientReconnects = 0;
                    connection.Reconnecting += () =>
                    {
                        clientReconnects++;
                    };

                    await connection.Start(host).OrTimeout();

                    // Give SSE an opportunity to reconnect
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    Assert.Equal(connection.State, ConnectionState.Connected);
                    Assert.Equal(connection.Transport.Name, "longPolling");
                    Assert.Equal(0, serverReconnects);
                    Assert.Equal(0, clientReconnects);
                }
            }
        }

        //[Fact(Skip = "Disabled IIS Express tests because they fail to initialize")]
        public async Task WebSocketsTransportFailsIfOnConnectedThrows()
        {
            using (ITestHost host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                var connection = CreateConnection(host, "/fall-back-throws");

                using (connection)
                {
                    await Assert.ThrowsAsync<AggregateException>(() => connection.Start(new WebSocketTransport()));
                }
            }
        }

        //[Fact(Skip = "Disabled IIS Express tests because they fail to initialize")]
        public async Task TransportConnectTimeoutDoesNotAddupOverNegotiateRequests()
        {
            using (ITestHost host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();
                var connection = CreateConnection(host, "/signalr");
                connection.TransportConnectTimeout = TimeSpan.FromSeconds(5);

                using (connection)
                {
                    await connection.Start();
                    var totalTransportConnectTimeout = ((Client.IConnection)connection).TotalTransportConnectTimeout;
                    connection.Stop();
                    await connection.Start();
                    Assert.Equal(((Client.IConnection)connection).TotalTransportConnectTimeout, totalTransportConnectTimeout);
                }
            }
        }

        //[Fact(Skip = "Disabled IIS Express tests because they fail to initialize")]
        public async Task ABlockedReceivedCallbackWillTriggerAnError()
        {
            using (ITestHost host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                using (var connection = CreateConnection(host, "/echo"))
                {
                    var wh = new TaskCompletionSource<object>();

                    connection.DeadlockErrorTimeout = TimeSpan.FromSeconds(1);
                    connection.Error += error =>
                    {
                        wh.TrySetResult(error);
                    };

                    await connection.Start();

                    // Ensure the received callback is actually called
                    await connection.Send("");

                    Assert.IsType<SlowCallbackException>(await wh.Task.OrTimeout(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ConnectionDisposeTriggersStop(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateConnection(host, "/signalr");

                using (connection)
                {
                    await connection.Start(host.Transport);
                    Assert.Equal(connection.State, Client.ConnectionState.Connected);
                }

                Assert.Equal(connection.State, Client.ConnectionState.Disconnected);
            }
        }


        [Theory]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task RequestHeadersSetCorrectly(HostType hostType, TransportType transportType)
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

                    await connection.Start(host.Transport);
                    var ignore = connection.Send("Hello");

                    // Assert
                    await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        public async Task RequestHeadersCanBeSetOnceConnected(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");
                var mre = new TaskCompletionSource<object>();

                using (connection)
                {
                    connection.Received += arg =>
                    {
                        JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                        Assert.Equal("test-header", (string)headers["testHeader"]);

                        mre.TrySetResult(null);
                    };

                    await connection.Start(host.Transport);

                    connection.Headers.Add("test-header", "test-header");
                    var ignore = connection.Send("message");

                    // Assert
                    await mre.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }

            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ReconnectRequestPathEndsInReconnect(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var tcs = new TaskCompletionSource<bool>();
                var receivedMessage = false;

                host.Initialize(keepAlive: null,
                                connectionTimeout: 2,
                                disconnectTimeout: 6);

                using (var connection = CreateConnection(host, "/force-lp-reconnect/examine-reconnect"))
                {
                    connection.Received += reconnectEndsPath =>
                    {
                        if (!receivedMessage)
                        {
                            tcs.TrySetResult(reconnectEndsPath == "True");
                            receivedMessage = true;
                        }
                    };

                    await connection.Start(host.Transport);

                    // Wait for reconnect
                    Assert.True(await tcs.Task.OrTimeout());
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task ConnectionFunctionsCorrectlyAfterCallingStartMutlipleTimes(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateConnection(host, "/echo"))
                {
                    var tcs = new TaskCompletionSource<object>();
                    connection.Received += _ => tcs.TrySetResult(null);

                    // We're purposely calling Start() twice here
                    await connection.Start(host.TransportFactory()).OrTimeout();
                    await connection.Start(host.TransportFactory()).OrTimeout();

                    await connection.Send("test").OrTimeout();

                    // Wait for message to be received
                    await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        // Negotiation is handled per-transport (even though it uses common code) so we do this for each transport to make sure we didn't miss something
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Auto)]
        public async Task ConnectionFailsWithHelpfulErrorWhenAttemptingToConnectToAspNetCoreApp(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                using (var connection = CreateConnection(host, "/aspnetcore-signalr"))
                {
                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.Start(host.TransportFactory())).OrTimeout();
                    Assert.Equal(
                        "Detected a connection attempt to an ASP.NET Core SignalR Server. This client only supports connecting to an ASP.NET SignalR Server. See https://aka.ms/signalr-core-differences for details.",
                        ex.Message);
                }
            }
        }
    }
}
