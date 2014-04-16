using System;
using System.Globalization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class TransportFacts
    {
        [Fact]
        public void VerifyLastActiveSetsLastErrorIfConnectionExpired()
        {
            var mockConnection = new Mock<IConnection>();

            mockConnection.Setup(c => c.LastActiveAt).Returns(new DateTime(1));
            mockConnection.Setup(c => c.ReconnectWindow).Returns(new TimeSpan(42));

            var connection = mockConnection.Object;

            Assert.False(TransportHelper.VerifyLastActive(connection));

            var expectedMessage = 
                string.Format(CultureInfo.CurrentCulture, Resources.Error_ReconnectWindowTimeout,
                    connection.LastActiveAt, connection.ReconnectWindow);

            mockConnection.Verify(c => c.Stop(It.Is<TimeoutException>(e => e.Message == expectedMessage)));
        }
    }
}
