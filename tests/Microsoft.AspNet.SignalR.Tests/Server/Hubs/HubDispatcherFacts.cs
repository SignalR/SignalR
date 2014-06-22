using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class HubDispatcherFacts
    {
        public static IEnumerable<string[]> JSProxyUrls
        {
            get
            {
                return new List<string[]>()
                {
                    new []{"http://something/signalr/hubs"},
                    new []{"http://something/signalr/js"}
                };
            }
        }

        [Theory]
        [PropertyData("JSProxyUrls")]
        public void RequestingSignalrHubsUrlReturnsProxy(string proxyUrl)
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration());
            var request = GetRequestForUrl(proxyUrl);
            var response = new Mock<IResponse>();
            string contentType = null;
            var buffer = new List<string>();
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));

            // Act
            var context = new HostContext(request.Object, response.Object);
            dispatcher.Initialize(new DefaultDependencyResolver());
            dispatcher.ProcessRequest(context).Wait();

            // Assert
            Assert.Equal("application/javascript; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);
            Assert.False(buffer[0].StartsWith("throw new Error("));
        }

        [Theory]
        [PropertyData("JSProxyUrls")]
        public void RequestingSignalrHubsUrlWithTrailingSlashReturnsProxy(string proxyUrl)
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration());
            var request = GetRequestForUrl(proxyUrl);
            var response = new Mock<IResponse>();
            string contentType = null;
            var buffer = new List<string>();
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));

            // Act
            var context = new HostContext(request.Object, response.Object);
            dispatcher.Initialize(new DefaultDependencyResolver());
            dispatcher.ProcessRequest(context).Wait();

            // Assert
            Assert.Equal("application/javascript; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.NotNull(buffer[0]);
            Assert.False(buffer[0].StartsWith("throw new Error("));
        }

        [Theory]
        [PropertyData("JSProxyUrls")]
        public void RequestingSignalrHubsUrlWithJavaScriptProxiesDesabledDoesNotReturnProxy(string proxyUrl)
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration() { EnableJavaScriptProxies = false });
            var request = GetRequestForUrl(proxyUrl);
            var response = new Mock<IResponse>();
            string contentType = null;
            var buffer = new List<string>();
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));

            // Act
            var context = new HostContext(request.Object, response.Object);
            dispatcher.Initialize(new DefaultDependencyResolver());
            dispatcher.ProcessRequest(context).Wait();

            // Assert
            Assert.Equal("application/javascript; charset=UTF-8", contentType);
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].StartsWith("throw new Error("));
        }

        [Fact]
        public void DetailedErrorsAreDisabledByDefault()
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration());

            var request = GetRequestForUrl("http://something/signalr/send");
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"Error\",\"A\":[],\"I\":0}"}
                                                      }));
            request.Setup(m => m.ReadForm()).Returns(Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper()));

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.CancellationToken).Returns(CancellationToken.None);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.True(buffer.Count > 0);

            using (var reader = new StringReader(String.Join(String.Empty, buffer)))
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
            var dispatcher = new HubDispatcher(new HubConfiguration());

            var request = GetRequestForUrl("http://something/signalr/send");
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"ErrorTask\",\"A\":[],\"I\":0}"}
                                                      }));
            request.Setup(m => m.ReadForm()).Returns(Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper()));

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.CancellationToken).Returns(CancellationToken.None);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.True(buffer.Count > 0);
            
            using (var reader = new StringReader(String.Join(String.Empty, buffer)))
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
            var dispatcher = new HubDispatcher(new HubConfiguration() { EnableDetailedErrors = true });

            var request = GetRequestForUrl("http://something/signalr/send");
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"Error\",\"A\":[],\"I\":0}"}
                                                      }));
            request.Setup(m => m.ReadForm()).Returns(Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper()));

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.CancellationToken).Returns(CancellationToken.None);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.True(buffer.Count > 0);

            using (var reader = new StringReader(String.Join(String.Empty, buffer)))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Equal("Custom Error.", hubResponse.Error);
            }
        }

        [Fact]
        public void DetailedErrorsFromFaultedTasksCanBeEnabled()
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration() { EnableDetailedErrors = true });

            var request = GetRequestForUrl("http://something/signalr/send");
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(new NameValueCollection()
                                                      {
                                                          {"transport", "longPolling"},
                                                          {"connectionToken", "0"},
                                                          {"data", "{\"H\":\"ErrorHub\",\"M\":\"ErrorTask\",\"A\":[],\"I\":0}"}
                                                      }));

            request.Setup(m => m.ReadForm()).Returns(Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper()));

            string contentType = null;
            var buffer = new List<string>();

            var response = new Mock<IResponse>();
            response.SetupGet(m => m.CancellationToken).Returns(CancellationToken.None);
            response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(type => contentType = type);
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Callback<ArraySegment<byte>>(data => buffer.Add(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count)));
            response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

            // Act
            var context = new HostContext(request.Object, response.Object);
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            resolver.Register(typeof(ErrorHub), () => new ErrorHub());
            dispatcher.Initialize(resolver);

            dispatcher.ProcessRequest(context).Wait();

            var json = JsonSerializer.Create(new JsonSerializerSettings());


            // Assert
            Assert.Equal("application/json; charset=UTF-8", contentType);
            Assert.True(buffer.Count > 0);

            using (var reader = new StringReader(String.Join(String.Empty, buffer)))
            {
                var hubResponse = (HubResponse)json.Deserialize(reader, typeof(HubResponse));
                Assert.Equal("Custom Error from task.", hubResponse.Error);
            }
        }

        [Fact]
        public void DuplicateHubNamesThrows()
        {
            // Arrange
            var dispatcher = new HubDispatcher(new HubConfiguration());
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            qs["connectionData"] = @"[{name: ""foo""}, {name: ""Foo""}]";

            var mockHub = new Mock<IHub>();
            var mockHubManager = new Mock<IHubManager>();
            mockHubManager.Setup(m => m.GetHub("foo")).Returns(new HubDescriptor { Name = "foo", HubType = mockHub.Object.GetType() });

            var dr = new DefaultDependencyResolver();
            dr.Register(typeof(IHubManager), () => mockHubManager.Object);

            dispatcher.Initialize(dr);
            Assert.Throws<InvalidOperationException>(() => dispatcher.Authorize(request.Object));
        }

        private static Mock<IRequest> GetRequestForUrl(string url)
        {
            var request = new Mock<IRequest>();
            var uri = new Uri(url);
            request.Setup(m => m.Url).Returns(uri);
            request.Setup(m => m.LocalPath).Returns(uri.LocalPath);
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper());
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
