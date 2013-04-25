using System;
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            Assert.Throws<InvalidOperationException>(() => queue.Send(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void EnqueueWithoutOpenRaisesOnError()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);

            Assert.Throws<InvalidOperationException>(() => queue.Send(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void ErrorOnSendThrowsNextTime()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();

            Task task = queue.Send(_ =>
            {
                throw new InvalidOperationException();
            },
            null);

            Assert.Throws<AggregateException>(() => task.Wait());
            Assert.Throws<InvalidOperationException>(() => queue.Send(_ => TaskAsyncHelper.Empty, null));
        }


        [Fact(Timeout = 1000)]
        public void OpenAfterErrorRunsQueue()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();
            queue.Send(_ => { throw new InvalidOperationException(); }, null);

            queue.Open();

            queue.Send(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            var task = queue.Send(_ =>
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();
            queue.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Send(async _ =>
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();
            queue.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);
            queue.Send(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);
            queue.Send(async _ =>
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();
            queue.Send(async _ =>
            {
                await Task.Delay(50);
                x++;
            },
            null);

            Task t1 = queue.Send(async _ =>
            {
                await Task.Delay(50);
                await TaskAsyncHelper.FromError(new Exception());
            },
            null);

            Assert.Throws<AggregateException>(() => t1.Wait());

            queue.Open();

            Task t2 = queue.Send(async _ =>
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Open();
            queue.Send(_ => Task.Delay(50), null);
            queue.Close();
            queue.Open();
            Assert.Throws<InvalidOperationException>(() => queue.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void InitialToBufferingToOpenToSend()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.SetError(new Exception());
            queue.Open();
            queue.Send(async _ =>
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
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Close();
        }

        [Fact]
        public void OpenAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Close();
            queue.Open();
            Assert.Throws<InvalidOperationException>(() => queue.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void BufferAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var queue = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            queue.Close();
            queue.SetError(new Exception());
            Assert.Throws<Exception>(() => queue.Send(_ => TaskAsyncHelper.Empty, null));
        }
    }
}
