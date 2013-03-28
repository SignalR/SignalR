// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutTaskQueue
    {
        private TaskCompletionSource<object> _taskCompletionSource;
        private TaskQueue _queue;
        private Task _closeTask;
        private QueueState _state;
        private readonly int _size;

        private const int DefaultQueueSize = 100000;

        private static readonly Task _queueFullTask = TaskAsyncHelper.FromError(new InvalidOperationException(Resources.Error_TaskQueueFull));

        public ScaleoutTaskQueue()
            : this(DefaultQueueSize)
        {
        }

        public ScaleoutTaskQueue(int size)
        {
            _size = size;

            InitializeCore();
        }

        public void Open()
        {
            lock (this)
            {
                if (ChangeState(QueueState.Open))
                {
                    _taskCompletionSource.TrySetResult(null);
                }
            }
        }

        public Task Enqueue(Func<object, Task> send, object state)
        {
            lock (this)
            {
                if (_state == QueueState.Closed)
                {
                    // This will be faulted
                    return _closeTask;
                }

                // If Enqueue returns null it means the queue is full
                return _queue.Enqueue(send, state) ?? _queueFullTask;
            }
        }

        public void Buffer()
        {
            lock (this)
            {
                if (ChangeState(QueueState.Buffering))
                {
                    InitializeCore();
                }
            }
        }

        public void Close(Exception error)
        {
            lock (this)
            {
                if (ChangeState(QueueState.Closed))
                {                   
                    _closeTask = TaskAsyncHelper.FromError(error);
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
                // Attempt to drain the queue before creating the new one
                return new[] { AlwaysSucceed(_queue.Drain()), _taskCompletionSource.Task }.Then(() => { });
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
            Buffering,
            Closed,
        }
    }
}
