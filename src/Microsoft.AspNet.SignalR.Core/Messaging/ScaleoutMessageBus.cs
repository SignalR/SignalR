// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ScaleoutMessageBus : MessageBus
    {
        private readonly ConcurrentDictionary<string, IndexedDictionary> _streams = new ConcurrentDictionary<string, IndexedDictionary>();
        private readonly SipHashBasedStringEqualityComparer _sipHashBasedComparer = new SipHashBasedStringEqualityComparer(0, 0);
        private readonly TraceSource _trace;
        private readonly Lazy<TaskQueueWrapper[]> _sendQueues;
        private readonly TaskQueue _receiveQueue;

        protected ScaleoutMessageBus(IDependencyResolver resolver)
            : base(resolver)
        {
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ScaleoutMessageBus).Name];
            _sendQueues = new Lazy<TaskQueueWrapper[]>(() =>
            {
                var queue = new TaskQueueWrapper[StreamCount];
                for (int i = 0; i < queue.Length; i++)
                {
                    queue[i] = new TaskQueueWrapper(_trace);
                }

                return queue;
            });

            _receiveQueue = new TaskQueue();
        }

        protected override TraceSource Trace
        {
            get
            {
                return _trace;
            }
        }

        /// <summary>
        /// The number of streams can't change for the lifetime of this instance.
        /// </summary>
        protected virtual int StreamCount
        {
            get
            {
                return 1;
            }
        }

        private TaskQueueWrapper[] SendQueues
        {
            get
            {
                return _sendQueues.Value;
            }
        }

        /// <summary>
        /// Opens all queues for sending messages.
        /// </summary>
        protected void Open()
        {
            for (int i = 0; i < StreamCount; i++)
            {
                SendQueues[i].Open();
            }
        }

        /// <summary>
        /// Opens the specified queue for sending messages.
        /// <param name="streamIndex">The index of the stream to open.</param>
        /// </summary>
        protected void Open(int streamIndex)
        {
            SendQueues[streamIndex].Open();
        }

        /// <summary>
        /// Closes all queues for sending messages making all sends fail asynchronously.
        /// </summary>
        /// <param name="exception">The error that occurred.</param>
        protected void Close(Exception exception)
        {
            for (int i = 0; i < StreamCount; i++)
            {
                SendQueues[i].Close(exception);
            }
        }

        /// <summary>
        /// Closes the specified queue for sending messages making all sends fail asynchronously.
        /// </summary>
        /// <param name="streamIndex">The index of the stream to close.</param>
        /// <param name="exception">The error that occurred.</param>
        protected void Close(int streamIndex, Exception exception)
        {
            // Close the queue means that all further sends will fail
            SendQueues[streamIndex].Close(exception);
        }

        /// <summary>
        /// Buffers the specified queue up to the specified size before failing.
        /// </summary>
        /// <param name="streamIndex">The index of the stream to buffer.</param>
        /// <param name="size">The maximum number of items to queue before failing to enqueue.</param>
        protected void Buffer(int streamIndex, int size)
        {
            SendQueues[streamIndex].Buffer(size);
        }

        /// <summary>
        /// Queues up to the specified size before failing.
        /// </summary>
        /// <param name="size">The maximum number of items to queue before failing to enqueue.</param>
        protected void Buffer(int size)
        {
            for (int i = 0; i < StreamCount; i++)
            {
                SendQueues[i].Buffer(size);
            }
        }

        /// <summary>
        /// Sends messages to the backplane
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected virtual Task Send(IList<Message> messages)
        {
            // If we're only using a single stream then just send
            if (StreamCount == 1)
            {
                return SendQueues[0].Enqueue((state) => Send(0, (IList<Message>)state), messages);
            }

            var taskCompletionSource = new TaskCompletionSource<object>();

            // Group messages by source (connection id)
            var messagesBySource = messages.GroupBy(m => m.Source);

            SendImpl(messagesBySource.GetEnumerator(), taskCompletionSource);

            return taskCompletionSource.Task;
        }

        protected virtual Task Send(int streamIndex, IList<Message> messages)
        {
            throw new NotImplementedException();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We return a faulted tcs")]
        private void SendImpl(IEnumerator<IGrouping<string, Message>> enumerator, TaskCompletionSource<object> taskCompletionSource)
        {
        send:

            if (!enumerator.MoveNext())
            {
                taskCompletionSource.TrySetResult(null);
            }
            else
            {
                IGrouping<string, Message> group = enumerator.Current;

                // Get the channel index we're going to use for this message
                int index = (int)((uint)_sipHashBasedComparer.GetHashCode(group.Key) % StreamCount);

                Debug.Assert(index >= 0, "Hash function resulted in an index < 0.");

                Task sendTask = SendQueues[index].Enqueue(state => Send(index, group.ToArray()), null).Catch();

                if (sendTask.IsCompleted)
                {
                    try
                    {
                        sendTask.Wait();

                        goto send;

                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetUnwrappedException(ex);
                    }
                }
                else
                {
                    sendTask.Then((enumer, tcs) => SendImpl(enumer, tcs), enumerator, taskCompletionSource)
                            .ContinueWithNotComplete(taskCompletionSource);
                }
            }
        }

        /// <summary>
        /// Invoked when a payload is received from the backplane. There should only be one active call at any time.
        /// </summary>
        /// <param name="streamId">id of the stream</param>
        /// <param name="id">id of the payload within that stream</param>
        /// <param name="messages">List of messages associated</param>
        /// <returns></returns>
        protected Task OnReceived(string streamId, ulong id, IList<Message> messages)
        {
            return _receiveQueue.Enqueue(() => OnReceivedCore(streamId, id, messages));
        }


        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Called from derived class")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        private Task OnReceivedCore(string streamId, ulong id, IList<Message> messages)
        {
            Counters.ScaleoutMessageBusMessagesReceivedPerSec.IncrementBy(messages.Count);

            Trace.TraceInformation("OnReceived({0}, {1}, {2})", streamId, id, messages.Count);

            // Create a local dictionary for this payload
            var dictionary = new ConcurrentDictionary<string, LocalEventKeyInfo>();

            foreach (var m in messages)
            {
                // Get the payload info
                var info = dictionary.GetOrAdd(m.Key, _ => new LocalEventKeyInfo());

                // Save the min and max for this payload for later
                ulong localId = Save(m);

                // Set the topic pointer for this event key so we don't need to look it up later
                info.Store = Topics[m.Key].Store;

                info.MinLocal = Math.Min(localId, info.MinLocal);
                info.Count++;
            }

            // Create the mapping for this payload
            var mapping = new ScaleoutMapping(dictionary);

            // Get the stream for this payload
            var stream = _streams.GetOrAdd(streamId, _ => new IndexedDictionary());

            // Publish only after we've setup the mapping fully
            if (!stream.TryAdd(id, mapping))
            {
                Trace.TraceVerbose(Resources.Error_DuplicatePayloadsForStream, streamId);

                stream.Clear();

                stream.TryAdd(id, mapping);
            }

            // Schedule after we're done
            foreach (var eventKey in dictionary.Keys)
            {
                ScheduleEvent(eventKey);
            }

            return TaskAsyncHelper.Empty;
        }

        public override Task Publish(Message message)
        {
            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();

            // TODO: Buffer messages here and make it configurable
            return Send(new[] { message });
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, object, Task<bool>> callback, int messageBufferSize, object state)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, _streams, callback, messageBufferSize, Counters, state);
        }

        private class TaskQueueWrapper
        {
            private TaskCompletionSource<object> _taskCompletionSource;
            private TaskQueue _sendQueue;

            private readonly TraceSource _trace;

            private static readonly Task _queueFullTask = TaskAsyncHelper.FromError(new InvalidOperationException(Resources.Error_TaskQueueFull));

            public TaskQueueWrapper(TraceSource trace)
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

            public Task Enqueue(Func<object, Task> taskFunc, object state)
            {
                lock (this)
                {
                    // If Enqueue returns null it means the queue is full
                    return _sendQueue.Enqueue(taskFunc, state) ?? _queueFullTask;
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
}
