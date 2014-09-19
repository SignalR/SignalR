// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.
extern alias StoreClient;

using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using StoreClientResources = StoreClient::Microsoft.AspNet.SignalR.Client.Resources;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests
{
    // To run these tests you need to start Microsoft.AspNet.SignalR.Client.Store.TestHost first
    public class EndToEndTests
    {
        private const string HubUrl = "http://localhost:42424";

        [Fact]
        public async Task WebSocketSendReceiveTest()
        {
            const int MessageCount = 3;
            var sentMessages = new List<string>();
            var receivedMessages = new List<string>();

            using (var hubConnection = new HubConnection(HubUrl))
            {
                var wh = new ManualResetEventSlim();

                var proxy = hubConnection.CreateHubProxy("StoreWebSocketTestHub");
                proxy.On<string>("echo", m =>
                {
                    receivedMessages.Add(m);
                    if (receivedMessages.Count == MessageCount)
                    {
                        wh.Set();
                    }
                });

                await hubConnection.Start(new WebSocketTransport());

                for (var i = 0; i < MessageCount; i++)
                {
                    var message = "MyMessage" + i;
                    await proxy.Invoke("Echo", message);
                    sentMessages.Add(message);
                }

                await Task.Run(() => wh.Wait(5000));
            }

            Assert.Equal(sentMessages, receivedMessages);
        }

        [Fact]
        public async Task WebSocketReconnects()
        {
            var stateChanges = new List<KeyValuePair<ConnectionState, ConnectionState>>();

            using (var hubConnection = new HubConnection(HubUrl))
            {
                string receivedMessage = null;
                var messageReceivedWh = new ManualResetEventSlim();

                var proxy = hubConnection.CreateHubProxy("StoreWebSocketTestHub");
                proxy.On<string>("echo", m =>
                {
                    receivedMessage = m;
                    messageReceivedWh.Set();
                });

                hubConnection.StateChanged += stateChanged => stateChanges.Add(
                    new KeyValuePair<ConnectionState, ConnectionState>(stateChanged.OldState, stateChanged.NewState));

                var reconnectingInvoked = false;
                hubConnection.Reconnecting += () => reconnectingInvoked = true;

                var reconnectedWh = new ManualResetEventSlim();
                hubConnection.Reconnected += reconnectedWh.Set;

                await hubConnection.Start(new WebSocketTransport { ReconnectDelay = new TimeSpan(0, 0, 0, 500) });

                try
                {
                    await proxy.Invoke("ForceReconnect");
                }
                catch (InvalidOperationException)
                {
                }

                Assert.True(await Task.Run(() => reconnectedWh.Wait(5000)));
                Assert.True(reconnectingInvoked);
                Assert.Equal(ConnectionState.Connected, hubConnection.State);

                // TODO: this is a workaround to a race condition in WebSocket. 
                // Should be removed once the race in WebSockets is fixed
                await Task.Delay(200);

                await proxy.Invoke("Echo", "MyMessage");

                await Task.Run(() => messageReceivedWh.Wait(5000));
                Assert.Equal("MyMessage", receivedMessage);
            }

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Disconnected,
                        ConnectionState.Connecting),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connecting,
                        ConnectionState.Connected),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connected,
                        ConnectionState.Reconnecting),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Reconnecting,
                        ConnectionState.Connected),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connected,
                        ConnectionState.Disconnected),
                },
                stateChanges);
        }

        [Fact]
        public async Task WebSocketReconnectsIfConnectionLost()
        {
            string receivedMessage = null;

            using (var hubConnection = new HubConnection(HubUrl))
            {
                hubConnection.StateChanged += stateChange =>
                {
                    if (stateChange.OldState == ConnectionState.Connected &&
                        stateChange.NewState == ConnectionState.Reconnecting)
                    {
                        // Reverting quick timeout 
                        ((IConnection) hubConnection).KeepAliveData = new KeepAliveData(
                            timeoutWarning: TimeSpan.FromSeconds(30),
                            timeout: TimeSpan.FromSeconds(20),
                            checkInterval: TimeSpan.FromSeconds(2));
                    }
                };

                var reconnectedWh = new ManualResetEventSlim();
                hubConnection.Reconnected += reconnectedWh.Set;

                var messageReceivedWh = new ManualResetEventSlim();
                var proxy = hubConnection.CreateHubProxy("StoreWebSocketTestHub");
                proxy.On<string>("echo", m =>
                {
                    receivedMessage = m;
                    messageReceivedWh.Set();
                });

                await hubConnection.Start(new WebSocketTransport { ReconnectDelay = new TimeSpan(0, 0, 0, 500) });

                // Setting the values such that a timeout happens almost instantly
                ((IConnection) hubConnection).KeepAliveData = new KeepAliveData(
                    timeoutWarning: TimeSpan.FromSeconds(10),
                    timeout: TimeSpan.FromSeconds(0.5),
                    checkInterval: TimeSpan.FromSeconds(1)
                    );

                Assert.True(await Task.Run(() => reconnectedWh.Wait(5000)));

                // TODO: this is a workaround to a race condition in WebSocket. 
                // Should be removed once the race in WebSockets is fixed
                await Task.Delay(200);

                await proxy.Invoke("Echo", "MyMessage");

                Assert.True(await Task.Run(() => messageReceivedWh.Wait(5000)));
                Assert.Equal("MyMessage", receivedMessage);
            }
        }

        [Fact(Skip = "xUnit AccessViolationException https://github.com/xunit/xunit/issues/190 when running with MsBuild. " +
                     "Note: This test still can be run in VS.")]
        public async Task SendingMessageWhenTransportIsReconnectingThrows()
        {
            using (var hubConnection = new HubConnection(HubUrl))
            {
                var reconnectingWh = new ManualResetEventSlim();

                Task sendTask = null;
                hubConnection.Reconnecting += () =>
                {
                    sendTask = hubConnection.Send("data");
                    reconnectingWh.Set();
                };

                Exception reportedException = null;
                hubConnection.Error += e => reportedException = e;

                var reconnectedWh = new ManualResetEventSlim();
                hubConnection.Reconnected += reconnectedWh.Set;

                var proxy = hubConnection.CreateHubProxy("StoreWebSocketTestHub");
                await hubConnection.Start(new WebSocketTransport { ReconnectDelay = new TimeSpan(0, 0, 0, 500) });

                try
                {
                    await proxy.Invoke("ForceReconnect");
                }
                catch (InvalidOperationException)
                {
                }

                Assert.True(await Task.Run(() => reconnectingWh.Wait(5000)));
                Assert.NotNull(sendTask);
                Assert.True(sendTask.IsFaulted);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sendTask);

                Assert.Equal(
                    StoreClientResources.GetResourceString("Error_DataCannotBeSentDuringWebSocketReconnect"),
                    exception.Message);

                Assert.Same(exception, reportedException);
            }
        }
    }
}
