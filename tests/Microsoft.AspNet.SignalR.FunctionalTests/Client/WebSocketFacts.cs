using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class WebSocketFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void ClientCanReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    connection.Start(host.Transport).Wait();

                    var result = hub.InvokeWithTimeout<string>("ReturnLargePayload");

                    Assert.Equal(new string('a', 64 * 1024), result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public void ServerCannotReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    connection.Start(host.Transport).Wait();

                    Assert.Throws<Xunit.Sdk.TrueException>(() =>
                    {
                        hub.InvokeWithTimeout<string>("EchoReturn", new string('a', 64 * 1024));
                    });
                }
            }
        }
    }
}
