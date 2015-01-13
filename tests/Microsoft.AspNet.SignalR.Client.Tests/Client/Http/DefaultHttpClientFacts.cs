
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class DefaultHttpClientFacts
    {
        [Fact]
        public void CanPostLargeMessage()
        {
            var messageTail = new string('A', ushort.MaxValue);
            var encodedMessage = string.Empty;

            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, t) => encodedMessage = r.Content.ReadAsStringAsync().Result)
                .Returns(Task.FromResult(response));

            var mockHttpClient = new Mock<DefaultHttpClient> {CallBase = true};
            mockHttpClient.Protected()
                .Setup<HttpMessageHandler>("CreateHandler")
                .Returns(mockHttpHandler.Object);

            var httpClient = mockHttpClient.Object;
            httpClient.Initialize(Mock.Of<IConnection>());

            var postData = new Dictionary<string, string> { { "data", " ," + messageTail } };

            httpClient.Post("http://fake.url", r => { }, postData, isLongRunning: false);

            Assert.Equal("data=+%2c" + messageTail, encodedMessage);
        }
    }
}
