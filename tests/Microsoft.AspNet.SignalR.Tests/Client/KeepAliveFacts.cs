using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using System;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
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
            var monitor = new HeartbeatMonitor(connection.Object);
            var keepAliveData = new KeepAliveData();

            // Setting the values such that a warning is thrown almost instantly
            keepAliveData.TimeoutWarning = TimeSpan.FromSeconds(1);

            // Keeping this sufficiently large so that timeout doesn't occur
            keepAliveData.Timeout = TimeSpan.FromSeconds(20);

            connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
            connection.Setup(m => m.State).Returns(ConnectionState.Connected);

            // Act - Setting timespan to be greater than timeout warining but less than timeout
            monitor.Beat(TimeSpan.FromSeconds(5));

            // Assert
            Assert.True(monitor.HasBeenWarned);
            connection.Verify(m => m.OnTimeoutWarning(), Times.Once());
        }

        /// <summary>
        /// Test to check if the client attempts to reconnect after the timeout interval
        /// </summary>
        [Fact]
        public void ConnectionTimeoutTest()
        {
            // Arrange
            var connection = new Mock<Client.IConnection>();
            var monitor = new HeartbeatMonitor(connection.Object);
            var transport = new Mock<IClientTransport>();

            var keepAliveData = new KeepAliveData();

            // Setting the values such that a timeout happens almost instantly
            keepAliveData.TimeoutWarning = TimeSpan.FromSeconds(0.5);
            keepAliveData.Timeout = TimeSpan.FromSeconds(1);

            connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
            connection.Setup(m => m.State).Returns(ConnectionState.Connected);
            connection.Setup(m => m.Transport).Returns(transport.Object);

            // Act - Setting timespan to be greater then timeout
            monitor.Beat(TimeSpan.FromSeconds(5));

            // Assert
            Assert.True(monitor.Reconnecting);
            transport.Verify(m => m.LostConnection(connection.Object), Times.Once());
        }

        /// <summary>
        /// Test to check if the variables are correctly set when the client is connected
        /// </summary>
        [Fact]
        public void NormalConnectionTest()
        {
            // Arrange
            var connection = new Mock<Client.IConnection>();
            var monitor = new HeartbeatMonitor(connection.Object);
            var transport = new Mock<IClientTransport>();

            var keepAliveData = new KeepAliveData();

            // Setting the values such that a timeout or timeout warning isn't issued
            keepAliveData.TimeoutWarning = TimeSpan.FromSeconds(5);
            keepAliveData.Timeout = TimeSpan.FromSeconds(10);

            connection.Setup(m => m.KeepAliveData).Returns(keepAliveData);
            connection.Setup(m => m.State).Returns(ConnectionState.Connected);
            connection.Setup(m => m.Transport).Returns(transport.Object);

            // Act - Setting timespan to be less than timeout and timeout warning
            monitor.Beat(TimeSpan.FromSeconds(2));

            // Assert
            Assert.False(monitor.Reconnecting);
            Assert.False(monitor.HasBeenWarned);
        }
    }
}
