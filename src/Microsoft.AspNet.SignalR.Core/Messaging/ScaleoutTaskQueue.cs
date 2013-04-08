// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
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
        private readonly ScaleoutConfiguration _configuration;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;

        public ScaleoutTaskQueue(TraceSource trace, string tracePrefix, ScaleoutConfiguration configuration)
            : this(trace, tracePrefix, configuration, DefaultQueueSize)
        {
        }

        public ScaleoutTaskQueue(TraceSource trace, string tracePrefix, ScaleoutConfiguration configuration, int size)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _trace = trace;
            _tracePrefix = tracePrefix;
            _configuration = configuration;
            _size = size;

            InitializeCore();
        }

        public void Open()
        {
            lock (this)
            {
                // Do nothing if the state is closed
                if (_state == QueueState.Closed)
                {
                    return;
                }

                if (ChangeState(QueueState.Open))
                {
                    _error = null;

                    _taskCompletionSource.TrySetResult(null);
                }
            }
        }

        public Task Enqueue(Func<object, Task> send, object state)
        {
            lock (this)
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
                    NotifyError(new InvalidOperationException(Resources.Error_QueueNotOpen));
                }

                Task task = _queue.Enqueue(send, state);

                if (task == null)
                {
                    // The task is null if the queue is full
                    throw new InvalidOperationException(Resources.Error_TaskQueueFull);
                }

                return task.Catch((ex, obj) => ((ScaleoutTaskQueue)obj).SetError(ex.InnerException), this);
            }
        }

        public void SetError(Exception error)
        {
            lock (this)
            {
                Buffer();

                if (_configuration.RetryOnError)
                {
                    // Raise the error handler if there's one specified
                    if (_configuration.OnError != null)
                    {
                        _configuration.OnError(error);
                    }
                }
                else
                {
                    // Set the error if we aren't retrying on error
                    _error = error;
                }
            }
        }

        public void Close()
        {
            Task task = TaskAsyncHelper.Empty;

            lock (this)
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

        private void NotifyError(Exception error)
        {
            if (_configuration.RetryOnError)
            {
                if (_configuration.OnError != null)
                {
                    _configuration.OnError(error);
                }
            }
            else
            {
                throw error;
            }
        }

        private void Buffer()
        {
            lock (this)
            {
                if (ChangeState(QueueState.Buffering))
                {
                    InitializeCore();
                }
            }
        }

        private void InitializeCore()
        {
            Task task = DrainQueue();
            _queue = new TaskQueue(task, _size);
        }

        private Task DrainQueue()
        {
            EnsureQueueStarted();

            _taskCompletionSource = new TaskCompletionSource<object>();

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

        private enum QueueState
        {
            Initial,
            Open,
            Buffering,
            Closed
        }
    }
}
