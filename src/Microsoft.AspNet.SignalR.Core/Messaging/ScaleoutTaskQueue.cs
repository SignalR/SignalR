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
        private TaskQueue _sendQueue;
        private Task _queueTask;
        private QueueState _state;

        private readonly TraceSource _trace;

        private static readonly Task _queueFullTask = TaskAsyncHelper.FromError(new InvalidOperationException(Resources.Error_TaskQueueFull));

        public ScaleoutTaskQueue(TraceSource trace)
        {
            _trace = trace;

            InitializeCore();
        }

        public void Open()
        {
            lock (this)
            {
                if (ChangeState(QueueState.Open))
                {
                    _taskCompletionSource.TrySetResult(null);

                    _queueTask = _sendQueue.Drain();

                    _sendQueue = null;
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
                    return _queueTask;
                }

                if (_queueTask != null)
                {
                    return _queueTask.Then((s, o) => s(o), send, state);
                }

                // If Enqueue returns null it means the queue is full
                return _sendQueue.Enqueue(send, state) ?? _queueFullTask;
            }
        }

        public void Buffer(int size)
        {
            lock (this)
            {
                if (ChangeState(QueueState.Buffering))
                {
                    InitializeCore(size);
                }
            }
        }

        public void Close(Exception error)
        {
            lock (this)
            {
                if (ChangeState(QueueState.Closed))
                {
                    _sendQueue = null;

                    _queueTask = TaskAsyncHelper.FromError(error);
                }
            }
        }

        private void InitializeCore(int? size = null)
        {
            _taskCompletionSource = new TaskCompletionSource<object>();

            Task task = DrainQueue();

            if (size != null)
            {
                _sendQueue = new TaskQueue(task, size.Value);
            }
            else
            {
                _sendQueue = new TaskQueue(task);
            }
        }

        private Task DrainQueue()
        {
            if (_sendQueue != null)
            {
                // Attempt to drain the queue before creating the new one                
                return new[] { AlwaysSucceed(_sendQueue.Drain()), _taskCompletionSource.Task }.Then(() => { });
            }

            // Nothing to drain
            return _taskCompletionSource.Task;
        }

        private bool ChangeState(QueueState queueState)
        {
            QueueState oldState = _state;
            _state = queueState;
            return oldState != queueState;
        }

        private Task AlwaysSucceed(Task task)
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
