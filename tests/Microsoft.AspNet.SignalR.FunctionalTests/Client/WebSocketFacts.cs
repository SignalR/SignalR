using System;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class WebSocketFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ClientCanReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                try
                {
                    var hub = connection.CreateHubProxy("demo");

                    connection.Start(host.Transport).Wait();

                    var result = hub.InvokeWithTimeout<string>("ReturnLargePayload");

                    Assert.Equal(new string('a', 64 * 1024), result);
                }
                finally
                {
                    connection.Stop();
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ServerCannotReceiveMessagesOver64KBViaWebSockets(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                try
                {
                    var hub = connection.CreateHubProxy("EchoHub");

                    connection.Start(host.Transport).Wait();

                    TestUtilities.AssertUnwrappedException<InvalidOperationException>(() =>
                    {
                        hub.InvokeWithTimeout<string>("EchoReturn", new string('a', 64 * 1024));
                    });
                }
                finally
                {
                    connection.Stop();
                }
            }
        }
    }
}
