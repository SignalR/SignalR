using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core.Hubs
{
    public class HubContextFacts
    {
        [Fact]
        public void GroupThrowsNullExceptionWhenGroupNameIsNull()
        {
            Func<string, ClientHubInvocation, IList<string>, Task> send = (signal, value, exclude) => Task.Factory.StartNew(() => { });

            var serializer = new JsonNetSerializer();
            var counters = new PerformanceCounterManager();
            var connection = new Connection(new Mock<IMessageBus>().Object,
                    serializer,
                    "signal",
                    "connectonid",
                    new[] { "test" },
                    new string[] { },
                    new Mock<ITraceManager>().Object,
                    new AckHandler(completeAcksOnTimeout: false,
                                   ackThreshold: TimeSpan.Zero,
                                   ackInterval: TimeSpan.Zero),
                    counters,
                    new Mock<IProtectedData>().Object);

            var hubContext = new HubContext(send, "test", connection);

            Assert.Throws<ArgumentException>(() => hubContext.Clients.Group(null));
        }

        [Fact]
        public void ClientThrowsNullExceptionWhenConnectionIdIsNull()
        {
            Func<string, ClientHubInvocation, IList<string>, Task> send = (signal, value, exclude) => Task.Factory.StartNew(() => { });

            var serializer = new JsonNetSerializer();
            var counters = new PerformanceCounterManager();
            var connection = new Connection(new Mock<IMessageBus>().Object,
                    serializer,
                    "signal",
                    "connectonid",
                    new[] { "test" },
                    new string[] { },
                    new Mock<ITraceManager>().Object,
                    new AckHandler(completeAcksOnTimeout: false,
                                   ackThreshold: TimeSpan.Zero,
                                   ackInterval: TimeSpan.Zero),
                    counters,
                    new Mock<IProtectedData>().Object);

            var hubContext = new HubContext(send, "test", connection);

            Assert.Throws<ArgumentException>(() => hubContext.Clients.Client(null));
        }
    }
}
