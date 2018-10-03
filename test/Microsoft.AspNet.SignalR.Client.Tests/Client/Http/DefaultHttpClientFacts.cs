// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            var mockHttpClient = new Mock<DefaultHttpClient> { CallBase = true };
            mockHttpClient.Protected()
                .Setup<HttpMessageHandler>("CreateHandler")
                .Returns(mockHttpHandler.Object);

            var httpClient = mockHttpClient.Object;
            httpClient.Initialize(Mock.Of<IConnection>());

            var postData = new Dictionary<string, string> { { "data", " ," + messageTail } };

            httpClient.Post("http://fake.url", r => { }, postData, isLongRunning: false);

            Assert.Equal("data=+%2c" + messageTail, encodedMessage);
        }

        [Fact]
        public async Task GetResponseIsDisposedWhenWrapperDisposed()
        {
            var testHandler = new TestHttpMessageHandler();
            var client = new TestHttpClient(new HttpClient(testHandler));
            var requestTracker = new DisposeTrackingHttpRequestMessage();
            var response = new DisposeTrackingHttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = requestTracker,
            };
            testHandler.OnGet("/test", (r, ct) => Task.FromResult<HttpResponseMessage>(response));

            var resp = await client.Get("http://example.com/test", r => { }, isLongRunning: false);

            Assert.False(response.Disposed);
            Assert.False(requestTracker.Disposed);

            resp.Dispose();

            Assert.True(response.Disposed);
            Assert.True(requestTracker.Disposed);
        }

        [Fact]
        public async Task GetResponseIsDisposedIfResponseIsNonSuccessful()
        {
            var testHandler = new TestHttpMessageHandler();
            var client = new TestHttpClient(new HttpClient(testHandler));
            var requestTracker = new DisposeTrackingHttpRequestMessage();
            var response = new DisposeTrackingHttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = requestTracker,
            };
            testHandler.OnGet("/test", (r, ct) => Task.FromResult<HttpResponseMessage>(response));

            await Assert.ThrowsAsync<HttpClientException>(() => client.Get("http://example.com/test", r => { }, isLongRunning: false));

            Assert.True(response.Disposed);
            Assert.True(requestTracker.Disposed);
        }

        [Fact]
        public async Task PostResponseIsDisposedWhenWrapperDisposed()
        {
            var testHandler = new TestHttpMessageHandler();
            var client = new TestHttpClient(new HttpClient(testHandler));
            var requestTracker = new DisposeTrackingHttpRequestMessage();
            var response = new DisposeTrackingHttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = requestTracker,
            };
            testHandler.OnPost("/test", (r, ct) => Task.FromResult<HttpResponseMessage>(response));

            var resp = await client.Post("http://example.com/test", r => { }, isLongRunning: false);

            Assert.False(response.Disposed);
            Assert.False(requestTracker.Disposed);

            resp.Dispose();

            Assert.True(response.Disposed);
            Assert.True(requestTracker.Disposed);
        }

        [Fact]
        public async Task PostResponseIsDisposedIfResponseIsNonSuccessful()
        {
            var testHandler = new TestHttpMessageHandler();
            var client = new TestHttpClient(new HttpClient(testHandler));
            var requestTracker = new DisposeTrackingHttpRequestMessage();
            var response = new DisposeTrackingHttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = requestTracker,
            };
            testHandler.OnPost("/test", (r, ct) => Task.FromResult<HttpResponseMessage>(response));

            await Assert.ThrowsAsync<HttpClientException>(() => client.Post("http://example.com/test", r => { }, isLongRunning: false));

            Assert.True(response.Disposed);
            Assert.True(requestTracker.Disposed);
        }

        private class TestHttpClient : DefaultHttpClient
        {
            private readonly HttpClient _client;

            public TestHttpClient(HttpClient client)
            {
                _client = client;
            }

            private protected override HttpClient GetHttpClient(bool isLongRunning)
            {
                return _client;
            }
        }

        private class DisposeTrackingHttpRequestMessage : HttpRequestMessage
        {
            public bool Disposed { get; private set; }

            public DisposeTrackingHttpRequestMessage() : base()
            {
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        private class DisposeTrackingHttpResponseMessage : HttpResponseMessage
        {
            public bool Disposed { get; private set; }

            public DisposeTrackingHttpResponseMessage(HttpStatusCode statusCode) : base(statusCode)
            {
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
