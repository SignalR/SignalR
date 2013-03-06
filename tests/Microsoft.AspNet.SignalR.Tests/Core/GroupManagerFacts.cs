using System;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class GroupManagerFacts
    {
        [Fact]
        public void SendThrowsNullExceptionWhenGroupNameIsNull()
        {
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

            var grpManager = new GroupManager(connection, "foo");
            
            Assert.Throws<ArgumentNullException>(() => grpManager.Send(null, new object()));
        }
    }
}
