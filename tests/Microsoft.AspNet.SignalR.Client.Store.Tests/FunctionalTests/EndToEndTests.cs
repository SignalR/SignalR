
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Xunit;

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
            var receivedMessage = (string)null;
            var reconnectingInvoked = false;
            var stateChanges = new List<KeyValuePair<ConnectionState, ConnectionState>>();

            using (var hubConnection = new HubConnection(HubUrl))
            {
                var messageReceivedWh = new ManualResetEventSlim();
                var proxy = hubConnection.CreateHubProxy("StoreWebSocketTestHub");
                proxy.On<string>("echo", m =>
                {
                    receivedMessage = m;
                    messageReceivedWh.Set();
                });

                hubConnection.StateChanged += stateChanged => stateChanges.Add(
                    new KeyValuePair<ConnectionState, ConnectionState>(stateChanged.OldState, stateChanged.NewState));

                hubConnection.Reconnecting += () => reconnectingInvoked = true;
                var wh = new ManualResetEventSlim();
                hubConnection.Reconnected += wh.Set;

                await hubConnection.Start(new WebSocketTransport { ReconnectDelay = new TimeSpan(0, 0, 0, 500)});

                try
                {
                    await proxy.Invoke("ForceReconnect");
                }
                catch (InvalidOperationException)
                {
                }

                Assert.True(await Task.Run(() => wh.Wait(5000)));
                Assert.True(reconnectingInvoked);
                Assert.Equal(ConnectionState.Connected, hubConnection.State);

                await proxy.Invoke("Echo", "MyMessage");
                await Task.Run(() => wh.Wait(5000));
                Assert.Equal("MyMessage", receivedMessage);
            }

            Assert.Equal(
                new[]
                {
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Disconnected, ConnectionState.Connecting),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connecting, ConnectionState.Connected),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connected, ConnectionState.Reconnecting),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Reconnecting, ConnectionState.Connected),
                    new KeyValuePair<ConnectionState, ConnectionState>(ConnectionState.Connected, ConnectionState.Disconnected),
                },
                stateChanges);
        }
    }
}
