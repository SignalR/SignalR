using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class TransportFacts
    {
        [Theory]
        [InlineData("bob=12345", "&bob=12345")]
        [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
        [InlineData("", "")]
        [InlineData(null, "?transport=&connectionToken=")]
        [InlineData("?foo=bar", "?foo=bar")]
        [InlineData("?foo=bar&baz=bear", "?foo=bar&baz=bear")]
        [InlineData("&foo=bar", "&foo=bar")]
        [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
        public void GetReceiveQueryStringAppendsConnectionQueryString(string connectionQs, string expected)
        {
            var connection = new Connection("http://foo.com", connectionQs);
            connection.ConnectionToken = "";

            var urlQs = TransportHelper.GetReceiveQueryString(connection, null, "");

            Assert.True(urlQs.EndsWith(expected));
        }

        [Theory]
        [InlineData("bob=12345", "?bob=12345")]
        [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "?bob=12345&foo=leet&baz=laskjdflsdk")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("?foo=bar", "?foo=bar")]
        [InlineData("?foo=bar&baz=bear", "?foo=bar&baz=bear")]
        [InlineData("&foo=bar", "&foo=bar")]
        [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
        public void AppendCustomQueryStringAppendsConnectionQueryString(string connectionQs, string expected)
        {
            var connection = new Connection("http://foo.com", connectionQs);

            var urlQs = TransportHelper.AppendCustomQueryString(connection, "http://foo.com");

            Assert.Equal(urlQs, expected);
        }

        [Fact]
        public void ProcessResponseCapturesOnReceivedExceptions()
        {
            bool timedOut, disconnected;
            var ex = new Exception();
            var connection = new Mock<Client.IConnection>(MockBehavior.Strict);
            connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
            connection.Setup(c => c.OnError(ex));
            connection.Setup(c => c.UpdateLastKeepAlive());

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

            using (var mockStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(mockStream))
                {
                    sw.Write("{}");
                    sw.Flush();
                    mockStream.Position = 0;

                    response.Setup(r => r.GetStream()).Returns(mockStream);
                    response.Setup(r => r.Dispose());

                    httpClient.Setup(h => h.Post(It.IsAny<string>(),
                                                 It.IsAny<Action<Client.Http.IRequest>>(),
                                                 It.IsAny<IDictionary<string, string>>()))
                              .Returns(TaskAsyncHelper.FromResult(response.Object));

                    connection.Setup(c => c.Trace(TraceLevels.Messages, It.IsAny<string>(), It.IsAny<object[]>()));
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

                    httpBasedTransport.Object.Send(connection.Object, "").ContinueWith(sendTask =>
                    {
                        Assert.True(sendTask.IsFaulted);
                        Assert.IsType(typeof(AggregateException), sendTask.Exception);
                        Assert.Equal(ex, sendTask.Exception.InnerException);
                        Assert.True(wh.Wait(TimeSpan.FromSeconds(1)));
                    }).Wait();

                    response.VerifyAll();
                    httpClient.VerifyAll();
                    connection.VerifyAll();
                }
            }
        }
    }
}
