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

        [Fact]
        public async Task CanConnectToEndpointWhichProducesARedirectResponseWithAQueryString()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                // "/redirect-query-string2" -> "/signalr?name1=newValue&name3=value3"
                using (var connection = CreateHubConnection(host, path: "/redirect-query-string2"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    Assert.Equal("newValue", await hub.Invoke<string>("GetQueryStringValue", "name1"));
                    Assert.Equal("value3", await hub.Invoke<string>("GetQueryStringValue", "name3"));
                }
            }
        }

        [Fact]
        public async Task CanMergeRedirectQueryStringWithUserQueryString()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                // This will get passed through queryString param in the (Hub)Connection ctor.
                host.ExtraData["foo"] = "bar";

                // "/redirect-query-string2" -> "/signalr?name1=newValue&name3=value3"
                using (var connection = CreateHubConnection(host, path: "/redirect-query-string2"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    // Verify values set via (Hub)Connection ctor.
                    Assert.Equal("bar", await hub.Invoke<string>("GetQueryStringValue", "foo"));

                    // Verify values set by redirect.
                    Assert.Equal("newValue", await hub.Invoke<string>("GetQueryStringValue", "name1"));
                    Assert.Equal("value3", await hub.Invoke<string>("GetQueryStringValue", "name3"));

                    // Verify that (Hub)Connection.QueryString only contains what was specified by the user, not the redirect.
                    // IConnection.QueryString contains both, since this is what's used by the client to actually build URLs.
                    Assert.Contains("foo=bar", connection.QueryString);
                    Assert.Contains("foo=bar", ((Client.IConnection)connection).QueryString);

                    Assert.DoesNotContain("name1=newValue", connection.QueryString);
                    Assert.Contains("name1=newValue", ((Client.IConnection)connection).QueryString);
                }
            }
        }

        [Fact]
        public async Task OnlyPreservesLastRedirectsQueryString()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                // "/redirect-query-string"
                // -> "/redirect-query-string2?name1=value1&name2=value2"
                // -> "/signalr?name1=newValue&name3=value3&origName1={context.Request.Query["name1"]}"
                using (var connection = CreateHubConnection(host, path: "/redirect-query-string"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    // Verify the client preserves query string key-value pairs specified in the last RedirectUrl.
                    Assert.Equal("newValue", await hub.Invoke<string>("GetQueryStringValue", "name1"));
                    Assert.Equal("value3", await hub.Invoke<string>("GetQueryStringValue", "name3"));

                    // Verify the client used "name1=value1" from the first RedirectUrl for the next request in the redirect chain.
                    Assert.Equal("value1", await hub.Invoke<string>("GetQueryStringValue", "origName1"));

                    // Verify the client does not preserve a query string key-value pair only specified in an intermediate RedirectUrl.
                    Assert.Equal(null, await hub.Invoke<string>("GetQueryStringValue", "name2"));
                }
            }
        }

        [Fact]
        public async Task CanConnectToEndpointWhichProducesARedirectResponseWithAnInvalidQueryString()
        {
            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();

                // "/redirect-query-string-invalid" -> "/signalr?redirect=invalid&/?=/&"
                using (var connection = CreateHubConnection(host, path: "/redirect-query-string-invalid"))
                {
                    var hub = connection.CreateHubProxy("RedirectTestHub");

                    await connection.Start(host.TransportFactory());

                    Assert.Equal("invalid", await hub.Invoke<string>("GetQueryStringValue", "redirect"));
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
