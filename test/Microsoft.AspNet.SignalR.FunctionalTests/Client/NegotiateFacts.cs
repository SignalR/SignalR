using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class NegotiateFacts : HostedTest
    {
        [Fact]
        public async Task CanConnectToEndpointWhichProducesARedirectResponse()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host, path: "/redirect"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    // Verify we're connected by calling the echo method
                    var result = await hub.Invoke<string>("EchoReturn", "Hello, World!");

                    Assert.Equal("Hello, World!", result);
                }
            }
        }

        [Theory]
        [InlineData(TransportType.Auto)]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.Websockets)]
        public async Task TransportForwardsAccessTokenProvidedByRedirectResponse(TransportType transportType)
        {
            // HttpListener is needed to support Websockets.
            using (var host = CreateHost(HostType.HttpListener, TransportType.Auto))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host, path: "/redirect"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    // Verify we're connected by calling the echo method
                    var result = await hub.Invoke<string>("GetAccessToken");

                    Assert.Equal("TestToken", result);
                }
            }
        }

        [Fact]
        public async Task RedirectsAreLimitedToPreventInfiniteLooping()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host, path: "/redirect-loop"))
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => connection.Start(host.TransportFactory()).OrTimeout());
                }
            }
        }

        [Fact]
        public async Task DoesNotFollowRedirectIfProtocolVersionIsNot20()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host, path: "/redirect-old-proto"))
                {
                    // Should fail to connect.
                    await Assert.ThrowsAsync<TimeoutException>(() => connection.Start(host.TransportFactory()));
                }
            }
        }

        [Fact]
        public async Task ThrowsErrorProvidedByServerIfNegotiateResponseContainsErrorMessage()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host, path: "/negotiate-error"))
                {
                    // Should fail to connect.
                    var ex = await Assert.ThrowsAsync<StartException>(() => connection.Start(host.TransportFactory()));
                    Assert.Equal("Error message received from the server: 'Server-provided negotiate error message!'.", ex.Message);
                }
            }
        }
    }
}
