// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ScaleoutMessageBus : MessageBus
    {
        private readonly ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>> _streams = new ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>>();

        protected ScaleoutMessageBus(IDependencyResolver resolver)
            : base(resolver)
        {
        }

        /// <summary>
        /// Sends messages to the backplane
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected abstract Task Send(Message[] messages);

        /// <summary>
        /// Invoked when a payload is received from the backplane. There should only be one active call at any time.
        /// </summary>
        /// <param name="streamId">id of the stream</param>
        /// <param name="id">id of the payload within that stream</param>
        /// <param name="messages">List of messages associated</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Called from derived class")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected Task OnReceived(string streamId, ulong id, Message[] messages)
        {
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
            var stream = _streams.GetOrAdd(streamId, _ => new Linktionary<ulong, ScaleoutMapping>());

            // Publish only after we've setup the mapping fully
            stream.Add(id, mapping);

            // Schedule after we're done
            foreach (var eventKey in dictionary.Keys)
            {
                ScheduleEvent(eventKey);
            }

            return TaskAsyncHelper.Empty;
        }

        public override Task Publish(Message message)
        {
            // TODO: Buffer messages here and make it configurable
            return Send(new[] { message });
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, _streams, callback, messageBufferSize, Counters);
        }
    }
}
