using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BookSleeve;
using SignalR.Infrastructure;

namespace SignalR.Redis
{
    public class RedisSignalBus : ISignalBus
    {
        private RedisConnection _redisConnection;
        private RedisSubscriberConnection _subscriberConnection;
        private ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>> _handlers;

        public RedisSignalBus(RedisConnection redisConnection)
        {
            _handlers = new ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>>();
            _redisConnection = redisConnection;
            _subscriberConnection = _redisConnection.GetOpenSubscriberChannel();
            _subscriberConnection.MessageReceived += OnRedisMessageReceived;
        }

        private void OnRedisMessageReceived(string eventKey, byte[] messageBytes)
        {
            OnSignaled(eventKey);
        }

        private void OnSignaled(string eventKey)
        {
            //Get handlers for this event...
            SafeSet<EventHandler<SignaledEventArgs>> handlersForEvent;
            if (_handlers.TryGetValue(eventKey, out handlersForEvent))
            {
                Parallel.ForEach(handlersForEvent.GetSnapshot(), h => h(this, new SignaledEventArgs(eventKey)));
            }
        }

        public Task Signal(string eventKey)
        {
            //Run our local event handlers for the key and publish the event to anyone listening.
            return
                Task.Factory.StartNew(() =>
                {
                    OnSignaled(eventKey);
                    _redisConnection.Publish(eventKey, "", true).Wait();
                });
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            //Get or create the bag of handlers for the event named {eventKey}
            var eventHandlersForKey = _handlers.GetOrAdd(eventKey, new SafeSet<EventHandler<SignaledEventArgs>>());

            //Determine whether we need to create a new redis subscription.
            var newRedisSubscriptionRequired = eventHandlersForKey.GetSnapshot().Count() == 0;

            //Add the handler to the bag and create the redis subscription if necessary
            eventHandlersForKey.Add(handler);
            if (newRedisSubscriptionRequired)
            {
                _subscriberConnection.Subscribe(eventKey);
            }
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            SafeSet<EventHandler<SignaledEventArgs>> eventHandlersForKey;
            if (_handlers.TryGetValue(eventKey, out eventHandlersForKey))
            {
                //We had a subscription, remove the handler.
                eventHandlersForKey.Remove(handler);

                //If there are no more handlers for this event unsubscribe our redis connection.
                if (eventHandlersForKey.GetSnapshot().Count() == 0)
                {
                    _handlers.TryRemove(eventKey, out eventHandlersForKey);
                    _subscriberConnection.Unsubscribe(eventKey);
                }
            }
        }
    }
}