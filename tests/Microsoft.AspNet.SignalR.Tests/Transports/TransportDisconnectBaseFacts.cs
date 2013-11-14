using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Transports
{
    public class TransportDisconnectBaseFacts
    {
        [Theory]
        [InlineData("false", true)]
        [InlineData("FALSE", true)]
        [InlineData("faLSe", true)]
        [InlineData("true", false)]
        [InlineData("1", false)]
        [InlineData("-1", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void SkipTrackingReturnsCorrectValueBasedOnQueryString(string queryStringValue, bool expectedValue)
        {
            // Arrange
            var transportHeartbeat = new Mock<ITransportHeartbeat>();
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var traceManager = new Mock<ITraceManager>();
            var queryString = new NameValueCollectionWrapper(new NameValueCollection { { "__track", queryStringValue } });
            var request = new Mock<IRequest>();
            request.SetupGet(r => r.QueryString).Returns(queryString);
            var response = new Mock<IResponse>();
            var hostContext = new Mock<HostContext>(request.Object, response.Object);

            // Act
            var connection = new Mock<TransportDisconnectBase>(hostContext.Object, transportHeartbeat.Object, perfCounters.Object, traceManager.Object);

            // Assert
            Assert.Equal(connection.Object.SkipTracking, expectedValue);
        }
    }
}
