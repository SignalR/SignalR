using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutStreamManagerFacts
    {
        [Fact]
        public void StreamManagerValidatesScaleoutConfig()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var config = new ScaleoutConfiguration();

            config.QueueBehavior = QueuingBehavior.Always;
            config.MaxQueueLength = 0;

            Assert.Throws<InvalidOperationException>(() => new ScaleoutStreamManager((int x, IList<Message> list) => { return TaskAsyncHelper.Empty; },
                (int x, ulong y, ScaleoutMessage msg) => { }, 0, new TraceSource("Stream Manager"), perfCounters, config));
        }
    }
}
