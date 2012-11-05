using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class ForeverTransportFacts
    {
        [Fact]
        public void SendUrlTriggersReceivedEvent()
        {
            var tcs = new TaskCompletionSource<string>();
            var request = new Mock<IRequest>();
            var form = new NameValueCollection();
            form["data"] = "This is my data";
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Form).Returns(form);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/send"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object)
            {
                CallBase = true
            };

            transport.Object.Received = data =>
            {
                tcs.TrySetResult(data);
                return TaskAsyncHelper.Empty;
            };

            transport.Object.ProcessRequest(transportConnection.Object).Wait();

            Assert.Equal("This is my data", tcs.Task.Result);
        }

        [Fact]
        public void AbortUrlTriggersConnectionAbort()
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/abort"));
            string abortedConnectionId = null;
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            transportConnection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()))
                               .Callback<ConnectionMessage>(m =>
                               {
                                   abortedConnectionId = m.Signal;
                                   var command = m.Value as Command;
                                   Assert.NotNull(command);
                                   Assert.Equal(CommandType.Abort, command.Type);
                               })
                               .Returns(TaskAsyncHelper.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object)
            {
                CallBase = true
            };

            transport.Object.ProcessRequest(transportConnection.Object).Wait();

            Assert.Equal("1", abortedConnectionId);
        }
    }
}
