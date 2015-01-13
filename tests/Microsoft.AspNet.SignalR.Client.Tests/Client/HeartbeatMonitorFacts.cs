using System;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class HeartbeatMonitorFacts
    {
        [Fact]
        public void ReconnectedClearsTimedOutAndHasBeenWarnedFlags()
        {
            var mockTransport = new Mock<IClientTransport>();
            mockTransport.Setup(t => t.SupportsKeepAlive).Returns(true);

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Transport).Returns(mockTransport.Object);
            mockConnection.Setup(c => c.KeepAliveData).Returns(new KeepAliveData(new TimeSpan(0, 0, 9)));
            mockConnection.Setup(c => c.State).Returns(ConnectionState.Connected);

            using (var monitor = new HeartbeatMonitor(mockConnection.Object, new object(), new TimeSpan(1, 0, 0)))
            {
                monitor.Start();
                // sets TimedOut flag
                monitor.Beat(new TimeSpan(0, 10, 0));
                // sets HasBeenWarned flag
                monitor.Beat(new TimeSpan(0, 0, 7));
                Assert.True(monitor.TimedOut);
                Assert.True(monitor.HasBeenWarned);

                monitor.Reconnected();
                Assert.False(monitor.TimedOut);
                Assert.False(monitor.HasBeenWarned);
            }
        }
    }
}
