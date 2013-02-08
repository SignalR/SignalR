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
                var mre = new ManualResetEvent(false);
                host.Initialize();
                var connection = CreateConnection(host.Url + "/timeout-recon");

                connection.Reconnected += () =>
                {
                    mre.Set();
                };

                connection.Start(host.Transport).Wait();
                connection.KeepAliveData.LastKeepAlive = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(2));
                connection.KeepAliveData.TimeoutWarning = TimeSpan.FromSeconds(0.5);
                connection.KeepAliveData.Timeout = TimeSpan.FromSeconds(1);
                
                // Assert
                Assert.True(mre.WaitOne(TimeSpan.FromSeconds(10)));

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
                var mre = new ManualResetEvent(false);
                host.Initialize();
                var connection = CreateConnection(host.Url + "/timeout-recon");

                connection.TimeoutWarning += () =>
                {
                    mre.Set();
                };

                connection.Start(host.Transport).Wait();

                connection.KeepAliveData.LastKeepAlive = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(2));
                connection.KeepAliveData.TimeoutWarning = TimeSpan.FromSeconds(1);
                connection.KeepAliveData.Timeout = TimeSpan.FromSeconds(5);

                // Assert
                Assert.True(mre.WaitOne(TimeSpan.FromSeconds(10)));

                // Clean-up
                mre.Dispose();
                connection.Stop();
            }
        }

    }
}
