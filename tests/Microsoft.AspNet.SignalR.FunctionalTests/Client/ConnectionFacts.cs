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

                connection.Received += (arg) =>
                {
                    JObject headers = JsonConvert.DeserializeObject<JObject>(arg);
                    if (transportType != TransportType.Websockets)
                    {
                        Assert.Equal("referer", (string)headers["refererHeader"]);
                    }
                    Assert.Equal("test-header", (string)headers["testHeader"]);
                    tcs.SetResult(null);
                };

                connection.Error += e => tcs.SetException(e);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void RequestHeadersCannotBeSetOnceConnected(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize();
                var connection = CreateConnection(host, "/examine-request");

                connection.Start(host.Transport).Wait();

                var ex = Assert.Throws<InvalidOperationException>(() => connection.Headers.Add("test-header", "test-header"));
                Assert.Equal("Request headers cannot be set after the connection has started.", ex.Message);

                // Clean-up
                connection.Stop();
            }
        }
    }
}
