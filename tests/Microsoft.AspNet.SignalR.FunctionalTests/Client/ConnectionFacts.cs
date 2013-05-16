using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ConnectionFacts : HostedTest
    {
        [Theory]
        [InlineData("1337.0", HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData("1337.0", HostType.Memory, TransportType.LongPolling)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.LongPolling)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData("1337.0", HostType.IISExpress, TransportType.Websockets)]
        [InlineData("0.1337", HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData("0.1337", HostType.Memory, TransportType.LongPolling)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.LongPolling)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData("0.1337", HostType.IISExpress, TransportType.Websockets)]
        public void ConnectionFailsToStartWithInvalidOldProtocol(string protocolVersion, HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = CreateConnection(host, "/signalr");

                connection.Protocol = new Version(protocolVersion);

                using (connection)
                {
                    connection.Start(host.Transport).ContinueWith(task => {
                        Assert.True(task.IsFaulted);
                    }).Wait();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ConnectionDisposeTriggersStop(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = CreateConnection(host,"/signalr");

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
                    connection.Received += (arg) =>
                    {
                        JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                        if (transportType != TransportType.Websockets)
                        {
                            Assert.Equal("referer", (string)headers["refererHeader"]);
                        }

                        Assert.Equal("test-header", (string)headers["testHeader"]);
                        Assert.Equal("user-agent", (string)headers["userAgentHeader"]);

                        tcs.TrySetResult(null);
                    };

                    connection.Error += e => tcs.TrySetException(e);

                    connection.Headers.Add("test-header", "test-header");
                    connection.Headers.Add(System.Net.HttpRequestHeader.UserAgent.ToString(), "user-agent");

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void RequestHeadersCannotBeSetOnceConnected(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");

                using (connection)
                {
                    connection.Start(host.Transport).Wait();

                    var ex = Assert.Throws<InvalidOperationException>(() => connection.Headers.Add("test-header", "test-header"));
                    Assert.Equal("Request headers cannot be set after the connection has started.", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
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

                var connection = CreateConnection(host, "/examine-reconnect");

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
    }
}
