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
        private readonly SipHashBasedStringEqualityComparer _sipHashBasedComparer = new SipHashBasedStringEqualityComparer(0, 0);
        private readonly ScaleOutConfiguration _configuration;
        private readonly TraceSource _trace;
        private readonly Lazy<ScaleoutStreamManager> _streamManager;

        protected ScaleoutMessageBus(IDependencyResolver resolver)
            : this(resolver, null)
        {

        }

        protected ScaleoutMessageBus(IDependencyResolver resolver, ScaleOutConfiguration configuation)
            : base(resolver)
        {
            _configuration = configuation ?? new ScaleOutConfiguration();
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ScaleoutMessageBus).Name];
            _streamManager = new Lazy<ScaleoutStreamManager>(() => new ScaleoutStreamManager(Send, OnReceivedCore, StreamCount));
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

        private ScaleoutStreamManager StreamManager
        {
            get
            {
                return _streamManager.Value;
            }
        }

        /// <summary>
        /// Opens the specified queue for sending messages.
        /// <param name="streamIndex">The index of the stream to open.</param>
        /// </summary>
        protected void Open(int streamIndex)
        {
            StreamManager.Open(streamIndex);
        }

        /// <summary>
        /// Closes the specified queue for sending messages making all sends fail asynchronously.
        /// </summary>
        /// <param name="streamIndex">The index of the stream to close.</param>
        /// <param name="exception">The error that occurred.</param>
        protected void OnError(int streamIndex, Exception exception)
        {
            if (_configuration.RetryOnError)
            {
                StreamManager.Buffer(streamIndex);
            }
            if (_configuration.OnError != null)
            {
                _configuration.OnError(exception);
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
                return StreamManager.Send(0, messages);
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

                Task sendTask = StreamManager.Send(index, group.ToArray()).Catch();

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
        /// <param name="streamIndex">id of the stream</param>
        /// <param name="id">id of the payload within that stream</param>
        /// <param name="messages">List of messages associated</param>
        /// <returns></returns>
        protected void OnReceived(int streamIndex, ulong id, IList<Message> messages)
        {
            StreamManager.OnReceived(streamIndex, id, messages);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Called from derived class")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        private void OnReceivedCore(int streamIndex, ulong id, IList<Message> messages)
        {
            Counters.ScaleoutMessageBusMessagesReceivedPerSec.IncrementBy(messages.Count);

            _trace.TraceInformation("OnReceived({0}, {1}, {2})", streamIndex, id, messages.Count);

            var localMapping = new LocalEventKeyInfo[messages.Count];
            var keys = new HashSet<string>();

            for (var i = 0; i < messages.Count; ++i)
            {
                Message message = messages[i];

                keys.Add(message.Key);

                ulong localId = Save(message);
                MessageStore<Message> messageStore = Topics[message.Key].Store;

                localMapping[i] = new LocalEventKeyInfo(message.Key, localId, messageStore);
            }

            // Get the stream for this payload
            ScaleoutMappingStore store = StreamManager.Streams[streamIndex];

            // Publish only after we've setup the mapping fully
            store.Add(id, localMapping);

            // Schedule after we're done
            foreach (var eventKey in keys)
            {
                ScheduleEvent(eventKey);
            }
        }

        public override Task Publish(Message message)
        {
            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();

            // TODO: Implement message batching here
            return Send(new[] { message });
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, object, Task<bool>> callback, int messageBufferSize, object state)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, StreamManager.Streams, callback, messageBufferSize, Counters, state);
        }
    }
}
