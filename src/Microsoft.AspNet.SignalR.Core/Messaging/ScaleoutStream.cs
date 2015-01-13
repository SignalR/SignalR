// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutStream
    {
        private TaskCompletionSource<object> _taskCompletionSource;
        private static Task _initializeDrainTask;
        private TaskQueue _queue;
        private StreamState _state;
        private Exception _error;

        private readonly int _size;
        private readonly QueuingBehavior _queueBehavior;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;
        private readonly IPerformanceCounterManager _perfCounters;

        private readonly object _lockObj = new object();

        public ScaleoutStream(TraceSource trace, string tracePrefix, QueuingBehavior queueBehavior, int size, IPerformanceCounterManager performanceCounters)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }

            _trace = trace;
            _tracePrefix = tracePrefix;
            _size = size;
            _perfCounters = performanceCounters;

            _queueBehavior = queueBehavior;

            InitializeCore();
        }

        private bool UsingTaskQueue
        {
            get
            {
                // Either you're always queuing or you're only queuing initially and you're in the initial state
                return _queueBehavior == QueuingBehavior.Always ||
                       (_queueBehavior == QueuingBehavior.InitialOnly && _state == StreamState.Initial);
            }
        }

        public void Open()
        {
            lock (_lockObj)
            {
                bool usingTaskQueue = UsingTaskQueue;

                StreamState previousState;
                if (ChangeState(StreamState.Open, out previousState))
                {
                    _perfCounters.ScaleoutStreamCountOpen.Increment();
                    _perfCounters.ScaleoutStreamCountBuffering.Decrement();

                    _error = null;

                    if (usingTaskQueue)
                    {
                        EnsureQueueStarted();

                        if (previousState == StreamState.Initial && _queueBehavior == QueuingBehavior.InitialOnly)
                        {
                            _initializeDrainTask = Drain(_queue, _trace);
                        }
                    }
                }
            }
        }

        public Task Send(Func<object, Task> send, object state)
        {
            lock (_lockObj)
            {
                if (_error != null)
                {
                    throw _error;
                }

                // If the queue is closed then stop sending
                if (_state == StreamState.Closed)
                {
                    throw new InvalidOperationException(Resources.Error_StreamClosed);
                }

                var context = new SendContext(this, send, state);

                if (_initializeDrainTask != null && !_initializeDrainTask.IsCompleted)
                {
                    // Wait on the draining of the queue before proceeding with the send
                    // NOTE: Calling .Wait() here is safe because the task wasn't created on an ASP.NET request thread
                    //       and thus has no captured sync context
                    _initializeDrainTask.Wait();
                }

                if (UsingTaskQueue)
                {
                    Task task = _queue.Enqueue(Send, context);

                    if (task == null)
                    {
                        // The task is null if the queue is full
                        throw new InvalidOperationException(Resources.Error_TaskQueueFull);
                    }

                    // Always observe the task in case the user doesn't handle it
                    return task.Catch(_trace);
                }

                return Send(context);
            }
        }

        public void SetError(Exception error)
        {
            Trace(TraceEventType.Error, "Error has happened with the following exception: {0}.", error);

            lock (_lockObj)
            {
                _perfCounters.ScaleoutErrorsTotal.Increment();
                _perfCounters.ScaleoutErrorsPerSec.Increment();

                Buffer();

                _error = error;
            }
        }

        public void Close()
        {
            Task task = TaskAsyncHelper.Empty;

            lock (_lockObj)
            {
                if (ChangeState(StreamState.Closed))
                {
                    _perfCounters.ScaleoutStreamCountOpen.RawValue = 0;
                    _perfCounters.ScaleoutStreamCountBuffering.RawValue = 0;

                    if (UsingTaskQueue)
                    {
                        // Ensure the queue is started
                        EnsureQueueStarted();

                        // Drain the queue to stop all sends
                        task = Drain(_queue, _trace);
                    }
                }
            }

            if (UsingTaskQueue)
            {
                // Block until the queue is drained so no new work can be done
                task.Wait();
            }
        }

        private static Task Send(object state)
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

                ctx.Stream.Trace(TraceEventType.Error, "Send failed: {0}", ex);

                lock (ctx.Stream._lockObj)
                {
                    // Set the queue into buffering state
                    ctx.Stream.SetError(ex.InnerException);

                    // Otherwise just set this task as failed
                    ctx.TaskCompletionSource.TrySetUnwrappedException(ex);
                }
            },
            context,
            context.Stream._trace);

            return context.TaskCompletionSource.Task;
        }

        private void Buffer()
        {
            lock (_lockObj)
            {
                if (ChangeState(StreamState.Buffering))
                {
                    _perfCounters.ScaleoutStreamCountOpen.Decrement();
                    _perfCounters.ScaleoutStreamCountBuffering.Increment();

                    InitializeCore();
                }
            }
        }

        private void InitializeCore()
        {
            if (UsingTaskQueue)
            {
                Task task = DrainPreviousQueue();
                _queue = new TaskQueue(task, _size);
                _queue.QueueSizeCounter = _perfCounters.ScaleoutSendQueueLength;
            }
        }

        private Task DrainPreviousQueue()
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
                return _taskCompletionSource.Task.Then((q, t) => Drain(q, t), _queue, _trace);
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

        private bool ChangeState(StreamState newState)
        {
            StreamState oldState;
            return ChangeState(newState, out oldState);
        }

        private bool ChangeState(StreamState newState, out StreamState previousState)
        {
            previousState = _state;

            // Do nothing if the state is closed
            if (_state == StreamState.Closed)
            {
                return false;
            }

            if (_state != newState)
            {
                Trace(TraceEventType.Information, "Changed state from {0} to {1}", _state, newState);

                _state = newState;
                return true;
            }

            return false;
        }

        private static Task Drain(TaskQueue queue, TraceSource traceSource)
        {
            if (queue == null)
            {
                return TaskAsyncHelper.Empty;
            }

            var tcs = new TaskCompletionSource<object>();
            
            queue.Drain().Catch(traceSource).ContinueWith(task =>
            {
                tcs.SetResult(null);
            });

            return tcs.Task;
        }

        private void Trace(TraceEventType traceEventType, string value, params object[] args)
        {
            _trace.TraceEvent(traceEventType, 0, _tracePrefix + " - " + value, args);
        }

        private class SendContext
        {
            private readonly Func<object, Task> _send;
            private readonly object _state;

            public readonly ScaleoutStream Stream;
            public readonly TaskCompletionSource<object> TaskCompletionSource;

            public SendContext(ScaleoutStream stream, Func<object, Task> send, object state)
            {
                Stream = stream;
                TaskCompletionSource = new TaskCompletionSource<object>();
                _send = send;
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

        private enum StreamState
        {
            Initial,
            Open,
            Buffering,
            Closed
        }
    }
}
