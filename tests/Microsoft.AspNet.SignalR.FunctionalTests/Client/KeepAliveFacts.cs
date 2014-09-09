using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class KeepAliveFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task ReconnectionSuccesfulTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new AsyncManualResetEvent(false);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(2));

                    connection.Reconnected += () =>
                    {
                        mre.Set();
                    };

                    await connection.Start(host.Transport);

                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(10)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task SuccessiveTimeoutTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new AsyncManualResetEvent(false);
                host.Initialize(keepAlive: 5, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    connection.Reconnected += () =>
                    {
                        mre.Set();
                    };

                    await connection.Start(host.Transport);

                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromMilliseconds(300));

                    // Assert that Reconnected is called
                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(15)));

                    // Assert that Reconnected is called again
                    mre.Reset();
                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(15)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task OnConnectionSlowTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new AsyncManualResetEvent(false);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(21), TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5));

                    connection.ConnectionSlow += () =>
                    {
                        mre.Set();
                    };

                    await connection.Start(host.Transport);

                    // Assert
                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(15)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }
    }
}
