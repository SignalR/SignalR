using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using System.Collections.Specialized;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void AbortUrlTriggersContentTypeSet()
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/abort");
            var response = new Mock<IResponse>();
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            var transport = new WebSocketTransport(hostContext, null, heartBeat.Object, counters.Object, traceManager.Object, null, null);
            transport.ProcessRequest(transportConnection.Object).Wait();
            response.VerifySet(r => r.ContentType = It.IsAny<string>(), "ContentType not set");
        }
    }
}