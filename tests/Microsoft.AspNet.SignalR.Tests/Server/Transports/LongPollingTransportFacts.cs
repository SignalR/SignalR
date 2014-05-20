using System;
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

            var transportConnection = new Mock<ITransportConnection>();
            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Returns(DisposableAction.Empty);

            // Act
            transport.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.True(transport.ConnectTask.Wait(TimeSpan.FromSeconds(2)), "ConnectTask task not tripped");
            Assert.False(connected, "The Connected event should not be raised");
            Assert.False(reconnected, "The Reconnected event should not be raised");
        }

        private class TestLongPollingTransport : LongPollingTransport
        {
            private TestLongPollingTransport(
                HostContext context,
                JsonSerializer json,
                ITransportHeartbeat heartBeat,
                IPerformanceCounterManager counters,
                ITraceManager traceManager,
                IConfigurationManager configuarionManager)
                : base(context, json, heartBeat, counters, traceManager, configuarionManager)
            {
            }

            public static TestLongPollingTransport Create(string requestPath)
            {
                var request = new Mock<IRequest>();
                request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper());
                request.Setup(m => m.LocalPath).Returns(requestPath);

                var response = new Mock<IResponse>();
                response.Setup(m => m.Flush()).Returns(TaskAsyncHelper.Empty);

                var hostContext = new HostContext(request.Object, response.Object);
                var json = JsonUtility.CreateDefaultSerializer();
                var heartBeat = new Mock<ITransportHeartbeat>();
                var counters = new Mock<IPerformanceCounterManager>();
                var traceManager = new Mock<ITraceManager>();
                var configuarionManager = new Mock<IConfigurationManager>();

                return new TestLongPollingTransport(
                    hostContext,
                    json,
                    heartBeat.Object,
                    counters.Object,
                    traceManager.Object,
                    configuarionManager.Object);
            }

            public bool TestSuppressReconnect
            {
                get { return SuppressReconnect; }
            }
        }
    }
}
