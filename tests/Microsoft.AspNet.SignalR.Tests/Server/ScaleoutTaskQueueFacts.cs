﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutTaskQueueFacts
    {
        [Fact]
        public void EnqueueWithoutOpenThrows()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void EnqueueWithoutOpenRaisesOnError()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);

            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void ErrorOnSendThrowsNextTime()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();

            Task task = queue.Enqueue(_ =>
            {
                throw new InvalidOperationException();
            },
            null);

            Assert.Throws<AggregateException>(() => task.Wait());
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }


        [Fact(Timeout = 1000)]
        public void OpenAfterErrorRunsQueue()
        {
            int x = 0;
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();
            queue.Enqueue(_ => { throw new InvalidOperationException(); }, null);

            queue.Open();

            queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            var task = queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            task.Wait();

            Assert.Equal(2, x);
        }

        [Fact]
        public void CloseWhileQueueRuns()
        {
            int x = 0;
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Close();
            Assert.Equal(3, x);
        }

        [Fact]
        public void CloseWhileQueueRunsWithFailedTask()
        {
            int x = 0;
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Close();
            Assert.Equal(1, x);
        }

        [Fact]
        public void OpenQueueErrorOpenQueue()
        {
            int x = 0;
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();
            queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);

            Task t1 = queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);

            Assert.Throws<AggregateException>(() => t1.Wait());

            queue.Open();

            Task t2 = queue.Enqueue(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);

            t2.Wait();

            Assert.Equal(2, x);
        }
        [Fact]
        public void SendAfterCloseThenOpenRemainsClosed()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Open();
            queue.Enqueue(_ => Task.Delay(50), null);
            queue.Close();
            queue.Open();
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void InitialToBufferingToOpenToSend()
        {
            int x = 0;
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.SetError(new Exception());
            queue.Open();
            queue.Enqueue(async _ =>
            {
                await Task.Delay(20);
                x++;
            },
            null).Wait();

            Assert.Equal(1, x);
        }

        [Fact(Timeout = 1000)]
        public void InitialToClosed()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Close();
        }

        [Fact]
        public void OpenAfterClosedEnqueueThrows()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Close();
            queue.Open();
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void BufferAfterClosedEnqueueThrows()
        {
            var perfCounters = new Mock<IPerformanceCounterManager>();
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", perfCounters.Object);
            queue.Close();
            queue.SetError(new Exception());
            Assert.Throws<Exception>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }
    }
}
