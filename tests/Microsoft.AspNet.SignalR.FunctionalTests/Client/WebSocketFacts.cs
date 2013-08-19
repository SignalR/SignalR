using System;
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
    }
}
