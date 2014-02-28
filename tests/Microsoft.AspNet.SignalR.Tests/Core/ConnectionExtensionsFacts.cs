using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class ConnectionExtensionsFacts
    {
        [Fact]
        public void SendThrowsNullExceptionWhenConnectionIdIsNull()
        {
            var serializer = JsonUtility.CreateDefaultSerializer();
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

            Assert.Throws<ArgumentException>(() => connection.Send((string)null, new object()));
        }

        [Fact]
        public void SendThrowsNullExceptionWhenConnectionIdsAreNull()
        {
            var serializer = JsonUtility.CreateDefaultSerializer();
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

            Assert.Throws<ArgumentNullException>(() => connection.Send((IList<string>)null, new object()));
        }
    }
}
