using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ScaleoutMessageBus : MessageBus
    {
        private readonly ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>> _streamMappings = new ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>>();

        public ScaleoutMessageBus(IDependencyResolver resolver)
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
        protected Task OnReceived(string streamId, ulong id, Message[] messages)
        {
            var stream = _streamMappings.GetOrAdd(streamId, _ => new Linktionary<ulong, ScaleoutMapping>());

            var mapping = new ScaleoutMapping();
            stream.Add(id, mapping);

            foreach (var m in messages)
            {
                // Get the payload info
                var info = mapping.EventKeyMappings.GetOrAdd(m.Key, _ => new LocalEventKeyInfo());

                // Save the min and max for this payload for later
                ulong localId = Save(m);

                // Set the topic pointer for this event key so we don't need to look it up later
                info.Topic = _topics[m.Key];

                info.MinLocal = Math.Min(localId, info.MinLocal);
                info.Count++;
            }

            foreach (var eventKey in mapping.EventKeyMappings.Keys)
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

        protected override Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            return new ScaleoutSubscription(subscriber.Identity, subscriber.EventKeys, cursor, _streamMappings, callback, messageBufferSize, _counters);
        }
    }
}
