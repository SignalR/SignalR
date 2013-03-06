// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        private TaskCompletionSource<object> _connectTcs = new TaskCompletionSource<object>();
        private readonly object _lockObj = new object();

        protected ScaleoutMessageBus(IDependencyResolver resolver)
            : base(resolver)
        {
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ScaleoutMessageBus).Name];
        }

        protected override TraceSource Trace
        {
            get
            {
                return _trace;
            }
        }

        protected virtual int StreamCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected void Connect()
        {
            lock (_connectTcs)
            {
                _connectTcs.TrySetResult(null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected void Disconnect()
        {
            Disconnect(exception: null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected void Disconnect(Exception exception)
        {
            if (exception != null)
            {
                _connectTcs.TrySetUnwrappedException(exception);
            }
            else
            {
                lock (_lockObj)
                {
                    // Create a new tcs 
                    _connectTcs = new TaskCompletionSource<object>();
                }
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
                return Send(0, messages);
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
                int index = _sipHashBasedComparer.GetHashCode(group.Key) % StreamCount;

                Task sendTask = Send(index, group.ToArray()).Catch();

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
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Called from derived class")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected Task OnReceived(string streamId, ulong id, IList<Message> messages)
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
            lock (_lockObj)
            {
                return _connectTcs.Task.Then(bus => bus.Send(new[] { message }), this);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, object, Task<bool>> callback, int messageBufferSize, object state)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, _streams, callback, messageBufferSize, Counters, state);
        }
    }
}
