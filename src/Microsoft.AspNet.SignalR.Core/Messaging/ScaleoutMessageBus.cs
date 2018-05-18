// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// Common base class for scaleout message bus implementations.
    /// </summary>
    public abstract class ScaleoutMessageBus : MessageBus
    {
        private readonly SipHashBasedStringEqualityComparer _sipHashBasedComparer = new SipHashBasedStringEqualityComparer(0, 0);
        private readonly ITraceManager _traceManager;
        private readonly TraceSource _trace;
        private readonly Lazy<ScaleoutStreamManager> _streamManager;
        private readonly IPerformanceCounterManager _perfCounters;

        protected ScaleoutMessageBus(IDependencyResolver resolver, ScaleoutConfiguration configuration)
            : base(resolver)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _traceManager = resolver.Resolve<ITraceManager>();
            _trace = _traceManager["SignalR." + typeof(ScaleoutMessageBus).Name];
            _perfCounters = resolver.Resolve<IPerformanceCounterManager>();
            var maxScaloutMappings = resolver.Resolve<IConfigurationManager>().MaxScaleoutMappingsPerStream;
            _streamManager = new Lazy<ScaleoutStreamManager>(
                () => new ScaleoutStreamManager(Send, OnReceivedCore, StreamCount, _trace, _perfCounters, configuration, maxScaloutMappings));
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
        /// Closes the specified queue.
        /// <param name="streamIndex">The index of the stream to close.</param>
        /// </summary>
        protected void Close(int streamIndex)
        {
            StreamManager.Close(streamIndex);
        }

        /// <summary>
        /// Closes the specified queue for sending messages making all sends fail asynchronously.
        /// </summary>
        /// <param name="streamIndex">The index of the stream to close.</param>
        /// <param name="exception">The error that occurred.</param>
        protected void OnError(int streamIndex, Exception exception)
        {
            StreamManager.OnError(streamIndex, exception);
        }

        /// <summary>
        /// Sends messages to the backplane
        /// </summary>
        /// <param name="messages">The list of messages to send</param>
        /// <returns></returns>
        protected virtual Task Send(IList<Message> messages)
        {
            // If we're only using a single stream then just send
            if (StreamCount == 1)
            {
                return StreamManager.Send(0, messages);
            }

            var taskCompletionSource = new DispatchingTaskCompletionSource<object>();

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
        private void SendImpl(IEnumerator<IGrouping<string, Message>> enumerator, DispatchingTaskCompletionSource<object> taskCompletionSource)
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

                Task sendTask = StreamManager.Send(index, group.ToArray()).Catch(_trace);

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
        /// <param name="streamIndex">id of the stream.</param>
        /// <param name="id">id of the payload within that stream.</param>
        /// <param name="message">The scaleout message.</param>
        /// <returns></returns>
        protected virtual void OnReceived(int streamIndex, ulong id, ScaleoutMessage message)
        {
            StreamManager.OnReceived(streamIndex, id, message);
        }

        private void OnReceivedCore(int streamIndex, ulong id, ScaleoutMessage scaleoutMessage)
        {
            Counters.ScaleoutMessageBusMessagesReceivedPerSec.IncrementBy(scaleoutMessage.Messages.Count);

            _trace.TraceInformation("OnReceived({0}, {1}, {2})", streamIndex, id, scaleoutMessage.Messages.Count);
            TraceScaleoutMessages(id, scaleoutMessage);

            var localMapping = new LocalEventKeyInfo[scaleoutMessage.Messages.Count];
            var keys = new HashSet<string>();

            for (var i = 0; i < scaleoutMessage.Messages.Count; ++i)
            {
                Message message = scaleoutMessage.Messages[i];

                // Remember where this message came from
                message.MappingId = id;
                message.StreamIndex = streamIndex;

                keys.Add(message.Key);
                ulong localId = Save(message);

                _trace.TraceVerbose("Message id: {0}, stream : {1}, eventKey: '{2}' saved with local id: {3}",
                    id, streamIndex, message.Key, localId);

                MessageStore<Message> messageStore = Topics[message.Key].Store;

                localMapping[i] = new LocalEventKeyInfo(message.Key, localId, messageStore);
            }

            // Get the stream for this payload
            ScaleoutMappingStore store = StreamManager.Streams[streamIndex];

            // Publish only after we've setup the mapping fully
            store.Add(id, scaleoutMessage, localMapping);

            if (_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                _trace.TraceVerbose("Scheduling eventkeys: {0}", string.Join(",", keys));
            }

            // Schedule after we're done
            foreach (var eventKey in keys)
            {
                ScheduleEvent(eventKey);
            }
        }

        private void TraceScaleoutMessages(ulong id, ScaleoutMessage scaleoutMessage)
        {
            if (!_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                return;
            }

            foreach (var message in scaleoutMessage.Messages)
            {
                _trace.TraceVerbose("Received message {0}: '{1}' over ScaleoutMessageBus", id, message.GetString());
            }
        }

        public override Task Publish(Message message)
        {
            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();

            // TODO: Implement message batching here
            return Send(new[] { message });
        }

        protected override void Dispose(bool disposing)
        {
            // Close all streams
            for (int i = 0; i < StreamCount; i++)
            {
                Close(i);
            }

            base.Dispose(disposing);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, object, Task<bool>> callback, int messageBufferSize, object state)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, StreamManager.Streams, callback, messageBufferSize, _traceManager, Counters, state);
        }
    }
}
