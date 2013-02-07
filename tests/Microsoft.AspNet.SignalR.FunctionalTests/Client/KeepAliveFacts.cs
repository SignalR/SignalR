using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using System;
using System.Threading;
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
                bool reconnected = false;
                var mre = new ManualResetEvent(false);
                host.Initialize();
                var connection = CreateConnection(host.Url + "/timeout-recon");
                connection.Start(host.Transport).Wait();

                connection.Reconnected += () =>
                {
                    reconnected = true;
                    mre.Set();
                };

                connection.KeepAliveData.LastKeepAlive = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(2));
                connection.KeepAliveData.TimeoutWarning = TimeSpan.FromSeconds(0.5);
                connection.KeepAliveData.Timeout = TimeSpan.FromSeconds(1);
                
                // Assert
                mre.WaitOne();
                Assert.True(reconnected);

                // Clean-up
                mre.Dispose();
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void TimeoutWarningThrownTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                bool warningThrown = false;
                var mre = new ManualResetEvent(false);
                host.Initialize();
                var connection = CreateConnection(host.Url + "/timeout-recon");
                connection.Start(host.Transport).Wait();

                connection.TimeoutWarning += () =>
                {
                    warningThrown = true;
                    mre.Set();
                };

                connection.KeepAliveData.LastKeepAlive = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(2));
                connection.KeepAliveData.TimeoutWarning = TimeSpan.FromSeconds(1);
                connection.KeepAliveData.Timeout = TimeSpan.FromSeconds(5);

                // Assert
                mre.WaitOne();
                Assert.True(warningThrown);

                // Clean-up
                mre.Dispose();
                connection.Stop();
            }
        }

    }
}
