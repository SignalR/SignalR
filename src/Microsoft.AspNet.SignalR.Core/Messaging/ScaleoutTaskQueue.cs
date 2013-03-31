// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
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
        private readonly bool _attachErrorHandler;

        public ScaleoutTaskQueue(ScaleoutConfiguration configuration)
            : this(configuration, DefaultQueueSize)
        {
        }

        public ScaleoutTaskQueue(ScaleoutConfiguration configuration, int size)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _attachErrorHandler = configuration.OnError == null && !configuration.RetryOnError;
            _configuration = configuration;
            _size = size;

            InitializeCore();
        }

        public void Open()
        {
            lock (this)
            {
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

                if (_attachErrorHandler)
                {
                    return task.Catch((ex, obj) => ((ScaleoutTaskQueue)obj).SetError(ex), this);
                }

                return task;
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

        private void NotifyError(Exception error)
        {
            if (_configuration.RetryOnError)
            {
                throw error;
            }
            else if (_configuration.OnError != null)
            {
                _configuration.OnError(error);
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
            _taskCompletionSource = new TaskCompletionSource<object>();

            if (_queue != null)
            {
                // Drain the queue when the new queue is open
                return _taskCompletionSource.Task.Then(q => AlwaysSucceed(q.Drain()), _queue);
            }

            // Nothing to drain
            return _taskCompletionSource.Task;
        }

        private bool ChangeState(QueueState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                return true;
            }

            return false;
        }

        private static Task AlwaysSucceed(Task task)
        {
            var tcs = new TaskCompletionSource<object>();
            task.Catch().Finally(state => ((TaskCompletionSource<object>)state).SetResult(null), tcs);
            return tcs.Task;
        }

        private enum QueueState
        {
            Initial,
            Open,
            Buffering
        }
    }
}
