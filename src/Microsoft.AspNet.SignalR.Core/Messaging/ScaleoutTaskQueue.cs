// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutTaskQueue
    {
        private const int DefaultQueueSize = 100000;

        private TaskCompletionSource<object> _taskCompletionSource;
        private TaskQueue _queue;
        private QueueState _state;
        private Exception _error;

        private readonly int _size;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;
        private readonly IPerformanceCounterManager _perfCounters;

        private readonly object _lockObj = new object();

        public ScaleoutTaskQueue(TraceSource trace, string tracePrefix, IPerformanceCounterManager performanceCounters)
            : this(trace, tracePrefix, DefaultQueueSize, performanceCounters)
        {
        }

        public ScaleoutTaskQueue(TraceSource trace, string tracePrefix, int size, IPerformanceCounterManager performanceCounters)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }

            _trace = trace;
            _tracePrefix = tracePrefix;
            _size = size;
            _perfCounters = performanceCounters;

            InitializeCore();
        }

        public bool Open()
        {
            lock (_lockObj)
            {
                if (ChangeState(QueueState.Open))
                {
                    _error = null;

                    _taskCompletionSource.TrySetResult(null);

                    return true;
                }
                return false;
            }
        }

        public Task Enqueue(Func<object, Task> send, object state)
        {
            lock (_lockObj)
            {
                if (_error != null)
                {
                    throw _error;
                }

                // If the queue is closed then stop sending
                if (_state == QueueState.Closed)
                {
                    throw new InvalidOperationException(Resources.Error_QueueClosed);
                }

                if (_state == QueueState.Initial)
                {
                    throw new InvalidOperationException(Resources.Error_QueueNotOpen);
                }

                var context = new SendContext(this, send, _perfCounters, state);
                Task task = _queue.Enqueue(QueueSend, context);

                if (task == null)
                {
                    // The task is null if the queue is full
                    throw new InvalidOperationException(Resources.Error_TaskQueueFull);
                }

                // Always observe the task in case the user doesn't handle it
                return task.Catch();
            }
        }

        public bool SetError(Exception error)
        {
            lock (_lockObj)
            {
                var buffering = Buffer();

                _error = error;

                return buffering;
            }
        }

        public void Close()
        {
            Task task = TaskAsyncHelper.Empty;

            lock (_lockObj)
            {
                if (ChangeState(QueueState.Closed))
                {
                    // Ensure the queue is started
                    EnsureQueueStarted();

                    // Drain the queue to stop all sends
                    task = Drain(_queue);
                }
            }

            // Block until the queue is drained so no new work can be done
            task.Wait();
        }

        private static Task QueueSend(object state)
        {
            var context = (SendContext)state;

            context.InvokeSend().Then(tcs =>
            {
                // Complete the task if the send is successful
                tcs.TrySetResult(null);
            },
            context.TaskCompletionSource)
            .Catch((ex, obj) =>
            {
                var ctx = (SendContext)obj;

                ctx.Queue.Trace("Send failed: {0}", ex);

                lock (ctx.Queue._lockObj)
                {
                    // Set the queue into buffering state
                    if (ctx.Queue.SetError(ex.InnerException))
                    {        
                        ctx.PerformanceCounters.ScaleoutStreamCountOpen.Decrement();
                        ctx.PerformanceCounters.ScaleoutStreamCountBuffering.Increment();   
                    };

                    // Otherwise just set this task as failed
                    ctx.TaskCompletionSource.TrySetUnwrappedException(ex);
                }
            },
            context);

            return context.TaskCompletionSource.Task;
        }

        private bool Buffer()
        {
            lock (_lockObj)
            {
                if (ChangeState(QueueState.Buffering))
                {
                    InitializeCore();

                    return true;
                }
                return false;
            }
        }

        private void InitializeCore()
        {
            Task task = DrainQueue();
            _queue = new TaskQueue(task, _size);
            _queue.QueueSizeCounter = _perfCounters.ScaleoutSendQueueLength;
        }

        private Task DrainQueue()
        {
            // If the tcs is null or complete then create a new one
            if (_taskCompletionSource == null ||
                _taskCompletionSource.Task.IsCompleted)
            {
                _taskCompletionSource = new TaskCompletionSource<object>();
            }

            if (_queue != null)
            {
                // Drain the queue when the new queue is open
                return _taskCompletionSource.Task.Then(q => Drain(q), _queue);
            }

            // Nothing to drain
            return _taskCompletionSource.Task;
        }

        private void EnsureQueueStarted()
        {
            if (_taskCompletionSource != null)
            {
                _taskCompletionSource.TrySetResult(null);
            }
        }

        private bool ChangeState(QueueState newState)
        {
            // Do nothing if the state is closed
            if (_state == QueueState.Closed)
            {
                return false;
            }

            if (_state != newState)
            {
                Trace("Changed state from {0} to {1}", _state, newState);

                _state = newState;
                return true;
            }

            return false;
        }

        private static Task Drain(TaskQueue queue)
        {
            if (queue == null)
            {
                return TaskAsyncHelper.Empty;
            }

            var tcs = new TaskCompletionSource<object>();

            queue.Drain().Catch().ContinueWith(task =>
            {
                tcs.SetResult(null);
            });

            return tcs.Task;
        }

        private void Trace(string value, params object[] args)
        {
            _trace.TraceInformation(_tracePrefix + " - " + value, args);
        }

        private class SendContext
        {
            private readonly Func<object, Task> _send;
            private readonly object _state;
            
            public readonly ScaleoutTaskQueue Queue;
            public readonly TaskCompletionSource<object> TaskCompletionSource;
            public readonly IPerformanceCounterManager PerformanceCounters;

            public SendContext(ScaleoutTaskQueue queue, Func<object, Task> send, IPerformanceCounterManager performanceCounters, object state)
            {
                Queue = queue;
                TaskCompletionSource = new TaskCompletionSource<object>();
                _send = send;
                PerformanceCounters = performanceCounters;
                _state = state;
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception flows to the caller")]
            public Task InvokeSend()
            {
                try
                {
                    return _send(_state);
                }
                catch (Exception ex)
                {
                    return TaskAsyncHelper.FromError(ex);
                }
            }
        }

        private enum QueueState
        {
            Initial,
            Open,
            Buffering,
            Closed
        }
    }
}
