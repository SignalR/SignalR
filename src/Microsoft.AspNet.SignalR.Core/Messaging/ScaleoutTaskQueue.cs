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
        private TaskCompletionSource<object> _taskCompletionSource;
        private TaskQueue _sendQueue;

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
                _taskCompletionSource.TrySetResult(null);
            }
        }

        public Task Enqueue(Func<object, Task> send, object state)
        {
            lock (this)
            {
                // If Enqueue returns null it means the queue is full
                return _sendQueue.Enqueue(send, state) ?? _queueFullTask;
            }
        }

        public void Buffer(int size)
        {
            lock (this)
            {
                InitializeCore(size);
            }
        }

        public void Close(Exception error)
        {
            lock (this)
            {
                InitializeCore();

                _taskCompletionSource.TrySetException(error);
            }
        }

        private void InitializeCore(int? size = null)
        {
            DrainQueue();

            _taskCompletionSource = new TaskCompletionSource<object>();

            if (size != null)
            {
                _sendQueue = new TaskQueue(_taskCompletionSource.Task, size.Value);
            }
            else
            {
                _sendQueue = new TaskQueue(_taskCompletionSource.Task);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method should never throw")]
        private void DrainQueue()
        {
            if (_sendQueue != null)
            {
                try
                {
                    // Attempt to drain the queue before creating the new one
                    _sendQueue.Drain().Wait();
                }
                catch (Exception ex)
                {
                    _trace.TraceError("Draining failed: " + ex.GetBaseException());
                }
            }
        }
    }
}
