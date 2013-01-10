using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class HubDispatcherFacts
    {
        [Fact]
        public void RequestingSignalrHubsUrlReturnsProxy()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", enableJavaScriptProxies: true);
            var request = GetRequestForUrl("http://something/signalr/hubs");
            var response = new Mock<IResponse>();
            string contentType = null;
            var buffer = new List<string>();
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);
            
            // Act
            var context = new HostContext(request.Object, response.Object);
            dispatcher.Initialize(new DefaultDependencyResolver(), context);
            dispatcher.ProcessRequest(context).Wait();

            // Assert
            Assert.Equal("application/x-javascript", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);
        }

        [Fact]
        public void RequestingSignalrHubsUrlWithTrailingSlashReturnsProxy()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", enableJavaScriptProxies: true);
            var request = GetRequestForUrl("http://something/signalr/hubs/");
            var response = new Mock<IResponse>();
            string contentType = null;
            var buffer = new List<string>();
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            dispatcher.Initialize(new DefaultDependencyResolver(), context);
            dispatcher.ProcessRequest(context).Wait();

            // Assert
            Assert.Equal("application/x-javascript", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);
        }

        private static Mock<IRequest> GetRequestForUrl(string url)
        {
            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri(url));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection());
            request.Setup(m => m.Form).Returns(new NameValueCollection());
            return request;
        }
    }
}
