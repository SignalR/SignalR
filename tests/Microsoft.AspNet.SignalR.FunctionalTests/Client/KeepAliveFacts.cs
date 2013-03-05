using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class KeepAliveFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void ReconnectionSuccesfulTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null);
                var connection = CreateConnection(host, "/my-reconnect");

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
                connection.Stop();
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SuccessiveTimeoutTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null);
                var connection = CreateConnection(host, "/my-reconnect");

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
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void OnConnectionSlowTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new ManualResetEventSlim(false);
                host.Initialize(keepAlive: null);
                var connection = CreateConnection(host, "/my-reconnect");

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
                connection.Stop();
            }
        }
    }
}
