using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class LongPollingTransportFacts
    {
        [Fact]
        public void SupressReconnectsForRequestsNotEndingInReconnect()
        {
            // Arrange transports while specifying request paths
            var reconnectTransport = TestLongPollingTransport.Create("/reconnect");
            var pollTransport = TestLongPollingTransport.Create("/poll");
            var emptyPathTransport = TestLongPollingTransport.Create("/");

            // Assert
            Assert.False(reconnectTransport.TestSuppressReconnect);
            Assert.True(pollTransport.TestSuppressReconnect);
            Assert.True(emptyPathTransport.TestSuppressReconnect);
        }

        [Fact]
        public void EmptyPathDoesntTriggerReconnects()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create(requestPath: "/");

            var connected = false;
            var reconnected = false;

            transport.Connected = () =>
            {
                connected = true;
                return TaskAsyncHelper.Empty;
            };

            transport.Reconnected = () =>
            {
                reconnected = true;
                return TaskAsyncHelper.Empty;
            };

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.ConnectTask.Wait(TimeSpan.FromSeconds(2)), "ConnectTask task not tripped");
            Assert.False(connected, "The Connected event should not be raised");
            Assert.False(reconnected, "The Reconnected event should not be raised");
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONSends()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create("/send");

            // Act
            transport.Send(new object());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JsonMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPSends()
        {
            // Arrange
            // Make the transport think it is responding to a JSONP request
            var queryString = new NameValueCollection { { "callback", "foo" } };
            var transport = TestLongPollingTransport.Create("/send", queryString);

            // Act
            // JSONP send
            transport.Send(new object());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JavaScriptMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPolls()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create("/poll");

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JsonMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPPolls()
        {
            // Arrange
            // Make the transport think it is responding to a JSONP request
            var queryString = new NameValueCollection { { "callback", "foo" } };
            var transport = TestLongPollingTransport.Create("/poll", queryString);

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JavaScriptMimeType, transport.TestContentType.Result);
        }

        private static ITransportConnection CreateMockTransportConnection()
        {
            var transportConnection = new Mock<ITransportConnection>();
            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Returns(DisposableAction.Empty);
            return transportConnection.Object;
        }

        private class TestLongPollingTransport : LongPollingTransport
        {
            private TaskCompletionSource<string> _contentTypeTcs = new TaskCompletionSource<string>();

            private TestLongPollingTransport(
                HostContext context,
                JsonSerializer json,
                ITransportHeartbeat heartBeat,
                IPerformanceCounterManager counters,
                ITraceManager traceManager,
                IConfigurationManager configurationManager)
                : base(context, json, heartBeat, counters, traceManager, configurationManager)
            {
            }

            public static TestLongPollingTransport Create(
                string requestPath,
                NameValueCollection queryString = null)
            {
                TestLongPollingTransport transport = null;

                var request = new Mock<IRequest>();
                queryString = queryString ?? new NameValueCollection();
                queryString["messageId"] = queryString["messageId"] ?? string.Empty;
                request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(queryString));
                request.Setup(m => m.LocalPath).Returns(requestPath);

                var response = new Mock<IResponse>();
                response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);
                response.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(contentType =>
                {
                    transport._contentTypeTcs.SetResult(contentType);
                });

                var hostContext = new HostContext(request.Object, response.Object);
                var json = JsonUtility.CreateDefaultSerializer();
                var heartBeat = new Mock<ITransportHeartbeat>();
                var counters = new Mock<IPerformanceCounterManager>();
                var traceManager = new Mock<ITraceManager>();
                var configuarionManager = new Mock<IConfigurationManager>();

                transport = new TestLongPollingTransport(
                    hostContext,
                    json,
                    heartBeat.Object,
                    counters.Object,
                    traceManager.Object,
                    configuarionManager.Object);

                return transport;
            }

            public Task<string> TestContentType
            {
                get { return _contentTypeTcs.Task; }
            }

            public bool TestSuppressReconnect
            {
                get { return SuppressReconnect; }
            }
        }
    }
}
