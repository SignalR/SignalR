using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutTaskQueueFacts
    {
        [Fact]
        public void EnqueueWithoutOpenThrows()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration());
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => { throw new InvalidOperationException(); }, null));
        }

        [Fact]
        public void EnqueueWithoutOpenRaisesOnError()
        {
            var tcs = new TaskCompletionSource<object>();
            var config = new ScaleoutConfiguration()
            {
                RetryOnError = true,
                OnError = ex => tcs.SetException(ex)
            };

            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", config);

            queue.Enqueue(_ => { throw new InvalidOperationException(); }, null);

            Assert.Throws<AggregateException>(() => tcs.Task.Wait());
        }

        [Fact]
        public void ErrorOnSendThrowsNextTime()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration());
            queue.Open();
            queue.Enqueue(_ => { throw new InvalidOperationException(); }, null);
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void ErrorOnSendBuffers()
        {
            int x = 0;
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Open();
            queue.Enqueue(_ => { throw new InvalidOperationException(); }, null);
            queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            Assert.Equal(0, x);
        }

        [Fact]
        public void OpenAfterErrorRunsQueue()
        {
            int x = 0;
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Open();
            queue.Enqueue(_ => { throw new InvalidOperationException(); }, null);
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

            queue.Open();

            task.Wait();

            Assert.Equal(2, x);
        }

        [Fact]
        public void CloseWhileQueueRuns()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Open();
            queue.Enqueue(_ => Task.Delay(50), null);
            queue.Enqueue(_ => Task.Delay(100), null);
            queue.Enqueue(_ => Task.Delay(150), null);
            queue.Close();
        }

        [Fact]
        public void SendAfterCloseThenOpenRemainsClosed()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
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
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.SetError(new Exception());
            queue.Open();
            queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null).Wait();

            Assert.Equal(1, x);
        }

        [Fact]
        public void SendsDuringInitialThenBufferingShouldNotSend()
        {
            int x = 0;
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });

            Task task = queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            queue.SetError(new Exception());

            task.Wait(TimeSpan.FromSeconds(0.5));

            Assert.Equal(0, x);
        }

        [Fact(Timeout = 1000)]
        public void SendsBeforeBufferingShouldBeCaptured()
        {
            int x = 0;
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });

            queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            queue.SetError(new Exception());

            Task task = queue.Enqueue(_ =>
            {
                x++;
                return TaskAsyncHelper.Empty;
            },
            null);

            queue.Open();

            task.Wait();

            Assert.Equal(2, x);
        }

        [Fact(Timeout = 1000)]
        public void InitialToClosed()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Close();
        }

        [Fact]
        public void OpenAfterClosedEnqueueThrows()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Close();
            queue.Open();
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void BufferAfterClosedEnqueueThrows()
        {
            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", new ScaleoutConfiguration() { RetryOnError = true });
            queue.Close();
            queue.SetError(new Exception());
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(_ => TaskAsyncHelper.Empty, null));
        }

        [Fact]
        public void ThrowingFromErrorCallbackIsCaught()
        {
            var config = new ScaleoutConfiguration()
            {
                RetryOnError = true,
                OnError = ex =>
                {
                    throw new Exception();
                }
            };

            var queue = new ScaleoutTaskQueue(new TraceSource("Queue"), "0", config);
            queue.SetError(new Exception());
        }
    }
}
