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
    public class ScaleoutStreamFacts
    {
        [Fact]
        public void EnqueueWithoutOpenThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            Assert.Throws<InvalidOperationException>(() => stream.Send(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void EnqueueWithoutOpenRaisesOnError()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);

            Assert.Throws<InvalidOperationException>(() => stream.Send(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void ErrorOnSendThrowsNextTime()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            stream.Open();

            Task task = stream.Send(_ =>
            {
                throw new InvalidOperationException();
            },
            null);

            Assert.Throws<AggregateException>(() => task.Wait());
            Assert.Throws<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }


        [Fact(Timeout = 10000)]
        public void OpenAfterErrorRunsQueue()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
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

            task.Wait();

            Assert.Equal(2, x);
        }

        [Fact]
        public void CloseWhileQueueRuns()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
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
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
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
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
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
        public void SendAfterCloseThenOpenRemainsClosed()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            stream.Open();
            stream.Send(_ => Task.Delay(50), null);
            stream.Close();
            stream.Open();
            Assert.Throws<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void InitialToBufferingToOpenToSend()
        {
            int x = 0;
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
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

        [Fact(Timeout = 10000)]
        public void InitialToClosed()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            stream.Close();
        }

        [Fact]
        public void OpenAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            stream.Close();
            stream.Open();
            Assert.Throws<InvalidOperationException>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void BufferAfterClosedEnqueueThrows()
        {
            var perfCounters = new Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager();
            var stream = new ScaleoutStream(new TraceSource("Queue"), "0", 1000, perfCounters);
            stream.Close();
            stream.SetError(new Exception());
            Assert.Throws<Exception>(() => stream.Send(_ => TaskAsyncHelper.Empty, null));
        }
    }
}
