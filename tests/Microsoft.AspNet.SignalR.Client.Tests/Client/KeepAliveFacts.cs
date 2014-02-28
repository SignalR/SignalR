using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class KeepAliveFacts
    {
        /// <summary>
        /// Test to check if the user is warned of a potential connection loss
        /// </summary>
        [Fact]
        public void TimeoutWarningTest()
        {
            // Arrange
            var connection = new Mock<Client.IConnection>();
            var transport = new Mock<IClientTransport>();

            transport.Setup(m => m.SupportsKeepAlive).Returns(true);

            // Setting the values such that a warning is thrown almost instantly and a timeout doesn't occur
            var keepAliveData = new KeepAliveData(
                timeoutWarning: TimeSpan.FromSeconds(1),
                timeout: TimeSpan.FromSeconds(20),
                checkInterval: TimeSpan.FromSeconds(2)
            );

            using (var monitor = new HeartbeatMonitor(connection.Object, new object(), keepAliveData.CheckInterval))
            {
                connection.Setup(m => m.LastMessageAt).Returns(DateTime.UtcNow);
                connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
                connection.Setup(m => m.State).Returns(ConnectionState.Connected);
                connection.Setup(m => m.Transport).Returns(transport.Object);

                monitor.Start();

                // Act - Setting timespan to be greater than timeout warining but less than timeout
                monitor.Beat(TimeSpan.FromSeconds(5));

                // Assert
                Assert.True(monitor.HasBeenWarned);
                Assert.False(monitor.TimedOut);
                connection.Verify(m => m.OnConnectionSlow(), Times.Once());
            }
        }

        /// <summary>
        /// Test to check if the client attempts to reconnect after the timeout interval
        /// </summary>
        [Fact]
        public void ConnectionTimeoutTest()
        {
            // Arrange
            var connection = new Mock<Client.IConnection>();
            var transport = new Mock<IClientTransport>();

            transport.Setup(m => m.SupportsKeepAlive).Returns(true);

            // Setting the values such that a timeout happens almost instantly
            var keepAliveData = new KeepAliveData(
                timeoutWarning: TimeSpan.FromSeconds(10),
                timeout: TimeSpan.FromSeconds(1),
                checkInterval: TimeSpan.FromSeconds(2)
            );

            using (var monitor = new HeartbeatMonitor(connection.Object, new object(), keepAliveData.CheckInterval))
            {
                connection.Setup(m => m.LastMessageAt).Returns(DateTime.UtcNow);
                connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
                connection.Setup(m => m.State).Returns(ConnectionState.Connected);
                connection.Setup(m => m.Transport).Returns(transport.Object);

                monitor.Start();

                // Act - Setting timespan to be greater then timeout
                monitor.Beat(TimeSpan.FromSeconds(5));

                // Assert
                Assert.True(monitor.TimedOut);
                Assert.False(monitor.HasBeenWarned);
                transport.Verify(m => m.LostConnection(connection.Object), Times.Once());
            }
        }

        /// <summary>
        /// Test to check if the variables are correctly set when the client is connected
        /// </summary>
        [Fact]
        public void NormalConnectionTest()
        {
            // Arrange
            var connection = new Mock<Client.IConnection>();
            var transport = new Mock<IClientTransport>();

            // Setting the values such that a timeout or timeout warning isn't issued
            var keepAliveData = new KeepAliveData(
                timeoutWarning: TimeSpan.FromSeconds(5),
                timeout: TimeSpan.FromSeconds(10),
                checkInterval: TimeSpan.FromSeconds(2)
            );

            using (var monitor = new HeartbeatMonitor(connection.Object, new object(), keepAliveData.CheckInterval))
            {
                connection.Setup(m => m.LastMessageAt).Returns(DateTime.UtcNow);
                connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
                connection.Setup(m => m.State).Returns(ConnectionState.Connected);
                connection.Setup(m => m.Transport).Returns(transport.Object);

                monitor.Start();

                // Act - Setting timespan to be less than timeout and timeout warning
                monitor.Beat(TimeSpan.FromSeconds(2));

                // Assert
                Assert.False(monitor.TimedOut);
                Assert.False(monitor.HasBeenWarned);
            }
        }

        private class DummyTextWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
