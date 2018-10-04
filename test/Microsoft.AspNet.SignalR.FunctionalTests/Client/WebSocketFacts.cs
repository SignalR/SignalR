// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class WebSocketFacts : HostedTest
    {
        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ClientCanReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("ReturnLargePayload").OrTimeout(TimeSpan.FromSeconds(30));

                    Assert.Equal(new string('a', 64 * 1024), result);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ServerCannotReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    await connection.Start(host.Transport);

                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke<string>("EchoReturn", new string('a', 64 * 1024))).OrTimeout();
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task MaxIncomingWebSocketMessageSizeCanBeDisabled(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Disable max message size
                host.Initialize(maxIncomingWebSocketMessageSize: null);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    await connection.Start(host.Transport).OrTimeout();

                    var payload = new string('a', 64 * 1024);
                    var result = await hub.Invoke<string>("EchoReturn", payload).OrTimeout();

                    Assert.Equal(payload, result);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task MaxIncomingWebSocketMessageSizeCanBeIncreased(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Increase max message size
                host.Initialize(maxIncomingWebSocketMessageSize: 128 * 1024);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    await connection.Start(host.Transport);

                    var payload = new string('a', 64 * 1024);
                    var result = await hub.Invoke<string>("EchoReturn", payload).OrTimeout();

                    Assert.Equal(payload, result);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task MaxIncomingWebSocketMessageSizeCanBeReduced(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Reduce max message size
                host.Initialize(maxIncomingWebSocketMessageSize: 8 * 1024);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    await connection.Start(host.Transport);

                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke<string>("EchoReturn", new string('a', 8 * 1024)).OrTimeout());
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task SendingDuringWebSocketReconnectFails(HostType hostType, TransportType transportType)
        {
            var wh1 = new TaskCompletionSource<object>();
            var wh2 = new TaskCompletionSource<object>();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                disconnectTimeout: 6,
                                connectionTimeout: 2,
                                enableAutoRejoiningGroups: true);

                using (var connection = CreateHubConnection(host))
                {
                    var proxy = connection.CreateHubProxy("demo");

                    connection.Reconnecting += async () =>
                    {
                        try
                        {
                            await connection.Send("test");
                        }
                        catch (InvalidOperationException)
                        {
                            wh1.TrySetResult(null);
                        }

                        try
                        {
                            await proxy.Invoke("GetValue");
                        }
                        catch (InvalidOperationException)
                        {
                            wh2.TrySetResult(null);
                        }
                    };

                    await connection.Start(host.Transport);

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    await wh2.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }
    }
}
