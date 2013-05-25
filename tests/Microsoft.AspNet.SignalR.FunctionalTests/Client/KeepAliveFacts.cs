using System;
using System.Threading;
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
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        public void ReconnectionSuccesfulTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(2));

                    connection.Reconnected += () =>
                    {
                        mre.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    // Assert
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        public void SuccessiveTimeoutTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(2));

                    connection.Reconnected += () =>
                    {
                        mre.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    // Assert that Reconnected is called
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));

                    // Assert that Reconnected is called again
                    mre.Reset();
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        public void OnConnectionSlowTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(2));

                    connection.ConnectionSlow += () =>
                    {
                        mre.Set();
                    };

                    connection.Start(host.Transport).Wait();

                    // Assert
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }
    }
}
