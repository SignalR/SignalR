using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class AckHandlerFacts
    {
        [Fact]
        public void AcksLastingLongerThanThresholdAreCompleted()
        {
            var ackHandler = new AckHandler(completeAcksOnTimeout: true, 
                                            ackThreshold: TimeSpan.FromSeconds(1),
                                            ackInterval: TimeSpan.FromSeconds(1));

            Task task = ackHandler.CreateAck("foo");

            Thread.Sleep(TimeSpan.FromSeconds(5));

            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void TriggeredAcksAreCompleted()
        {
            var ackHandler = new AckHandler(completeAcksOnTimeout: false,
                                            ackThreshold: TimeSpan.Zero,
                                            ackInterval: TimeSpan.Zero);

            Task task = ackHandler.CreateAck("foo");

            Assert.True(ackHandler.TriggerAck("foo"));
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void UnregisteredAcksCantBeTriggered()
        {
            var ackHandler = new AckHandler(completeAcksOnTimeout: false,
                                            ackThreshold: TimeSpan.Zero,
                                            ackInterval: TimeSpan.Zero);

            Assert.False(ackHandler.TriggerAck("foo"));
        }
    }
}
