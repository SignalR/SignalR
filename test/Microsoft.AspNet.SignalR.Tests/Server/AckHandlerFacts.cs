// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class AckHandlerFacts
    {
        [Fact]
        public async Task AcksLastingLongerThanThresholdAreCompleted()
        {
            var ackHandler = new AckHandler(completeAcksOnTimeout: true,
                                            ackThreshold: TimeSpan.FromSeconds(1),
                                            ackInterval: TimeSpan.FromSeconds(1));

            await Assert.ThrowsAsync<TaskCanceledException>(() => ackHandler.CreateAck("foo").OrTimeout());
        }

        [Fact]
        public async Task TriggeredAcksAreCompleted()
        {
            var ackHandler = new AckHandler(completeAcksOnTimeout: false,
                                            ackThreshold: TimeSpan.Zero,
                                            ackInterval: TimeSpan.Zero);

            Task task = ackHandler.CreateAck("foo");

            Assert.True(ackHandler.TriggerAck("foo"));
            await task.OrTimeout();
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
