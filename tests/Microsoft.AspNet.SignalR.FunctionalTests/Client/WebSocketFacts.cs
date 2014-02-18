using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class WebSocketFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ClientCanReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var result = hub.InvokeWithTimeout<string>("ReturnLargePayload");

                    Assert.Equal(new string('a', 64 * 1024), result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ServerCannotReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    await connection.Start(host.Transport);

                    TestUtilities.AssertUnwrappedException<InvalidOperationException>(() =>
                    {
                        hub.Invoke<string>("EchoReturn", new string('a', 64 * 1024)).Wait();
                    });
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void MaxIncomingWebSocketMessageSizeCanBeDisabled(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Disable max message size
                host.Initialize(maxIncomingWebSocketMessageSize: null);

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    connection.Start(host.Transport).Wait();

                    var payload = new string('a', 64 * 1024);
                    var result = hub.InvokeWithTimeout<string>("EchoReturn", payload);

                    Assert.Equal(payload, result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void MaxIncomingWebSocketMessageSizeCanBeIncreased(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Increase max message size
                host.Initialize(maxIncomingWebSocketMessageSize: 128 * 1024);

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    connection.Start(host.Transport).Wait();

                    var payload = new string('a', 64 * 1024);
                    var result = hub.InvokeWithTimeout<string>("EchoReturn", payload);

                    Assert.Equal(payload, result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void MaxIncomingWebSocketMessageSizeCanBeReduced(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Reduce max message size
                host.Initialize(maxIncomingWebSocketMessageSize: 8 * 1024);

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    connection.Start(host.Transport).Wait();

                    TestUtilities.AssertUnwrappedException<InvalidOperationException>(() =>
                    {
                        hub.Invoke<string>("EchoReturn", new string('a', 8 * 1024)).Wait();
                    });
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task SendingDuringWebSocketReconnectFails(HostType hostType, TransportType transportType)
        {
            var wh1 = new ManualResetEventSlim();
            var wh2 = new ManualResetEventSlim();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                disconnectTimeout: 6,
                                connectionTimeout: 2,
                                enableAutoRejoiningGroups: true);

                using (HubConnection connection = CreateHubConnection(host))
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
                            wh1.Set();
                        }

                        try
                        {
                            await proxy.Invoke("GetValue");
                        }
                        catch (InvalidOperationException)
                        {
                            wh2.Set();
                        }
                    };

                    await connection.Start(host.Transport);

                    Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));
                    Assert.True(wh2.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }
    }
}
