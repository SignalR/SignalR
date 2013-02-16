using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class TransportFacts 
    {
        [Fact]
        public void ProcessResponseCapturesOnReceivedExceptions()
        {
            bool timedOut, disconnected;
            var ex = new Exception();
            var connection = new Mock<Client.IConnection>(MockBehavior.Strict);
            connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
            connection.Setup(c => c.OnError(ex));

            // PersistentResponse
            TransportHelper.ProcessResponse(connection.Object, "{\"M\":{}}", out timedOut, out disconnected);

            // HubResponse (WebSockets)
            TransportHelper.ProcessResponse(connection.Object, "{\"I\":{}}", out timedOut, out disconnected);

            connection.VerifyAll();
        }

        [Fact]
        public void SendCatchesOnReceivedExceptions()
        {
            var ex = new Exception();
            var wh = new ManualResetEventSlim();
            var response = new Mock<IResponse>(MockBehavior.Strict);
            var httpClient = new Mock<IHttpClient>(MockBehavior.Strict);
            var connection = new Mock<Client.IConnection>(MockBehavior.Strict);

            response.Setup(r => r.ReadAsString()).Returns("{}");

            httpClient.Setup(h => h.Post(It.IsAny<string>(),
                                         It.IsAny<Action<Client.Http.IRequest>>(),
                                         It.IsAny<IDictionary<string, string>>()))
                      .Returns(TaskAsyncHelper.FromResult(response.Object));

            connection.SetupGet(c => c.Url).Returns("");
            connection.SetupGet(c => c.QueryString).Returns("");
            connection.SetupGet(c => c.ConnectionToken).Returns("");
            connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
            connection.Setup(c => c.OnError(It.IsAny<AggregateException>())).Callback<Exception>(e =>
            {
                Assert.Equal(ex, e.InnerException);
                wh.Set();
            });

            var httpBasedTransport = new Mock<HttpBasedTransport>(httpClient.Object, "")
            {
                CallBase = true
            };

            var sendTask = httpBasedTransport.Object.Send(connection.Object, "");

            Assert.True(sendTask.IsFaulted);
            Assert.IsType(typeof(AggregateException), sendTask.Exception);
            Assert.Equal(ex, sendTask.Exception.InnerException);
            Assert.True(wh.Wait(TimeSpan.FromSeconds(1)));

            response.VerifyAll();
            httpClient.VerifyAll();
            connection.VerifyAll();
        }
    }
}
