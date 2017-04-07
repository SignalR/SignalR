﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutStreamFacts
    {
        [Fact]
        public async Task SendBeforeOpenDoesNotThrowWhenQueuingBehaviorInitialOnly()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.InitialOnly, 1000, perfCounters);

            var sendTask = stream.Send(_ => { return TaskAsyncHelper.Empty; }, null);

            stream.Open();

            await sendTask.OrTimeout();
        }

        [Fact]
        public void SendsBeforeOpenedRunOnceOpenedWhenQueuingBehaviorInitialOnly()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.InitialOnly, 1000, perfCounters);

            int x = 0;

            stream.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            Task task = stream.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            stream.Open();

            task.Wait();

            Assert.Equal(2, x);
        }

        [Theory]
        [InlineData(QueuingBehavior.InitialOnly, 1)]
        [InlineData(QueuingBehavior.Always, 2)]
        public void SendAfterOpenDoesBehavesCorrectlyForConfiguredQueuingBehavior(QueuingBehavior queuingBehavior, int expectedCounter)
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queueLengthCounter = new Mock<IPerformanceCounter>();
            var counterIncrementCount = 0;
            queueLengthCounter.Setup(c => c.Increment()).Callback(() => counterIncrementCount++);
            perfCounters.DefaultValue = DefaultValue.Mock;
            perfCounters.SetReturnsDefault(new NoOpPerformanceCounter());
            perfCounters.SetupGet(p => p.ScaleoutStreamCountOpen).Returns(Mock.Of<IPerformanceCounter>());
            perfCounters.SetupGet(p => p.ScaleoutStreamCountBuffering).Returns(Mock.Of<IPerformanceCounter>());
            perfCounters.Setup(pc => pc.ScaleoutSendQueueLength).Returns(queueLengthCounter.Object);

            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", queuingBehavior, 1000, perfCounters.Object);

            stream.Send(_ => TaskAsyncHelper.Empty, null);

            stream.Open();

            stream.Send(_ => TaskAsyncHelper.Empty, null).Wait();

            Assert.Equal(expectedCounter, counterIncrementCount);
        }

        [Fact]
        public void SendAfterOpenAndAfterErrorThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.InitialOnly, 1000, perfCounters);

            int x = 0;

            var firstSend = stream.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            stream.Open();
            
            // Wait on the first send to drain
            firstSend.Wait();

            stream.SetError(new Exception("Failed"));

            Assert.Throws<Exception>(() =>
            {
                stream.Send(_ =>
                {
                    x++;
                    return TaskAsyncHelper.Empty;
                },
                null).Wait();
            });

            Assert.Equal(1, x);
        }

        [Fact]
        public async Task ErrorOnSendThrowsNextTime()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();

            Task task = stream.Send(_ =>
            {
                throw new InvalidOperationException();
            },
            null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);
            await Assert.ThrowsAsync<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void OpenAfterErrorRunsQueue()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();
            stream.Send(_ => { throw new InvalidOperationException(); }, null);

            stream.Open();

            stream.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            var task = stream.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            Assert.True(task.Wait(10000));

            Assert.Equal(2, x);
        }

        [Fact]
        public void CloseWhileQueueRuns()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            stream.Close();
            Assert.Equal(3, x);
        }

        [Fact]
        public void CloseWhileQueueRunsWithFailedTask()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            stream.Close();
            Assert.Equal(1, x);
        }

        [Fact]
        public void OpenQueueErrorOpenQueue()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();
            stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);

            Task t1 = stream.Send(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);

            Assert.Throws<AggregateException>(() => t1.Wait());

            stream.Open();

            Task t2 = stream.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);

            t2.Wait();

            Assert.Equal(2, x);
        }
        [Fact]
        public async Task SendAfterCloseThenOpenRemainsClosed()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Open();
            var ignore = stream.Send(_ => Task.Delay(50), null);
            stream.Close();
            stream.Open();
            await Assert.ThrowsAsync<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void InitialToBufferingToOpenToSend()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.SetError(new Exception());
            stream.Open();
            stream.Send(async _ =>
            {
                await Task.Delay(20);
                x++;
            },
            null).Wait();

            Assert.Equal(1, x);
        }

        [Fact]
        public void InitialToClosed()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Close();
        }

        [Fact]
        public async Task OpenAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Close();
            stream.Open();
            await Assert.ThrowsAsync<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public async Task BufferAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", QueuingBehavior.Always, 1000, perfCounters);
            stream.Close();
            stream.SetError(new Exception());
            await Assert.ThrowsAsync<Exception>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }
    }
}
