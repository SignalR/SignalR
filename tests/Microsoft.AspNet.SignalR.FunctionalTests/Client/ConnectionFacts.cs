using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ConnectionFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ReconnectExceedingReconnectWindowDisconnectsWithFastBeatInterval(HostType hostType, TransportType transportType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: 9);
                var connection = CreateHubConnection(host);
                var disconnectWh = new ManualResetEventSlim();

                connection.Closed += () =>
                {
                    disconnectWh.Set();
                };

                SetReconnectDelay(host.Transport, TimeSpan.FromSeconds(15));

                connection.Start(host.Transport).Wait();

                // Set reconnect window to zero so the second we attempt to reconnect we can ensure that the reconnect window is verified.
                ((Client.IConnection)connection).ReconnectWindow = TimeSpan.FromSeconds(0);

                // Without this the connection start and reconnect can race with eachother resulting in a deadlock.
                Thread.Sleep(TimeSpan.FromSeconds(3));

                host.Shutdown();

                Assert.True(disconnectWh.Wait(TimeSpan.FromSeconds(15)), "Closed never fired");

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ReconnectExceedingReconnectWindowDisconnects(HostType hostType, TransportType transportType)
        {
            // Test cannot be async because if we do host.ShutDown() after an await the connection stops.

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = CreateHubConnection(host);

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

                connection.Stop();
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

                connection.Received += arg =>
                {
                    JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                    if (transportType != TransportType.Websockets)
                    {
                        Assert.Equal("referer", (string)headers["refererHeader"]);
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

                // Clean-up
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void RequestHeadersCanBeSetOnceConnected(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");
                var mre = new ManualResetEventSlim();

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

                connection.Stop();
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

                var connection = CreateConnection(host, "/force-lp-reconnect/examine-reconnect");

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

                // Clean-up
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void ConnectionFunctionsCorrectlyAfterCallingStartMutlipleTimes(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateConnection(host, "/echo");
                
                var tcs = new TaskCompletionSource<object>();
                connection.Received += _ => tcs.TrySetResult(null);

                // We're purposely calling Start().Wait() twice here
                connection.Start(host.TransportFactory()).Wait();
                connection.Start(host.TransportFactory()).Wait();

                connection.Send("test").Wait();

                // Wait for message to be received
                Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                
                connection.Stop();
            }
        }
    }
}
