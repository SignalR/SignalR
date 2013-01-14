using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class HubDispatcherFacts
    {
        [Fact]
        public void RequestingSignalrHubsUrlReturnsProxy()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration());
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
            Assert.False(buffer[0].StartsWith("throw new Error("));
        }

        [Fact]
        public void RequestingSignalrHubsUrlWithTrailingSlashReturnsProxy()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration());
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
            Assert.False(buffer[0].StartsWith("throw new Error("));
        }

        [Fact]
        public void RequestingSignalrHubsUrlWithJavaScriptProxiesDesabledDoesNotReturnProxy()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration() { EnableJavaScriptProxies = false });
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
            Assert.True(buffer[0].StartsWith("throw new Error("));
        }

        [Fact]
        public void DetailedErrorsAreDisabledByDefault()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration());
            
            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri("http://something/signalr/send"));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"Error\",\"A\":[],\"I\":0}"}
                                                      });
            request.Setup(m => m.Form).Returns(new NameValueCollection());

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.IsClientConnected).Returns(true);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver, context);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);
            
            using (var reader = new StringReader(buffer[0]))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Contains("ErrorHub.Error", hubResponse.Error);
                Assert.DoesNotContain("Custom", hubResponse.Error);
            }
        }

        [Fact]
        public void DetailedErrorsFromFaultedTasksAreDisabledByDefault()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration());

            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri("http://something/signalr/send"));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"ErrorTask\",\"A\":[],\"I\":0}"}
                                                      });
            request.Setup(m => m.Form).Returns(new NameValueCollection());

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.IsClientConnected).Returns(true);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver, context);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);

            using (var reader = new StringReader(buffer[0]))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Contains("ErrorHub.ErrorTask", hubResponse.Error);
                Assert.DoesNotContain("Custom", hubResponse.Error);
            }
        }

        [Fact]
        public void DetailedErrorsCanBeEnabled()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration() { EnableDetailedErrors = true });

            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri("http://something/signalr/send"));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"Error\",\"A\":[],\"I\":0}"}
                                                      });
            request.Setup(m => m.Form).Returns(new NameValueCollection());

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.IsClientConnected).Returns(true);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver, context);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);

            using (var reader = new StringReader(buffer[0]))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Equal("Custom Error.", hubResponse.Error);
            }
        }

        [Fact]
        public void DetailedErrorsFromFaultedTasksCanBeEnabled()
        {
            // Arrange
            var dispatcher = new HubDispatcher("/signalr", new HubConfiguration() { EnableDetailedErrors = true });

            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri("http://something/signalr/send"));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"ErrorTask\",\"A\":[],\"I\":0}"}
                                                      });
            request.Setup(m => m.Form).Returns(new NameValueCollection());

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.IsClientConnected).Returns(true);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.End()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver, context);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);

            using (var reader = new StringReader(buffer[0]))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Equal("Custom Error from task.", hubResponse.Error);
            }
        }

        private static Mock<IRequest> GetRequestForUrl(string url)
        {
            var request = new Mock<IRequest>();
            request.Setup(m => m.Url).Returns(new Uri(url));
            request.Setup(m => m.QueryString).Returns(new NameValueCollection());
            request.Setup(m => m.Form).Returns(new NameValueCollection());
            return request;
        }

        private class ErrorHub : Hub
        {
            public void Error()
            {
                throw new Exception("Custom Error.");
            }

            public async Task ErrorTask()
            {
                await TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(1));
                throw new Exception("Custom Error from task.");
            }
        }
    }
}
