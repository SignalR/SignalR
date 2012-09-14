using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : IMessageBus, IDisposable
    {
        private readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>();
        private readonly MessageBroker _broker;

        private const int DefaultMessageStoreSize = 5000;

        private readonly ITraceManager _trace;

        private readonly IPerformanceCounterWriter _counters;
        private readonly PerformanceCounter _msgsTotalCounter;
        private readonly PerformanceCounter _msgsPerSecCounter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>(), resolver.Resolve<IPerformanceCounterWriter>())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceManager"></param>
        public MessageBus(ITraceManager traceManager, IPerformanceCounterWriter performanceCounterWriter)
        {
            _trace = traceManager;
            
            _counters = performanceCounterWriter;
            _msgsTotalCounter = _counters.GetCounter(PerformanceCounters.MessageBusMessagesPublishedTotal);
            _msgsPerSecCounter = _counters.GetCounter(PerformanceCounters.MessageBusMessagesPublishedPerSec);

            _broker = new MessageBroker(_topics, _counters)
            {
                Trace = Trace
            };
        }

        private TraceSource Trace
        {
            get
            {
                return _trace["SignalR.MessageBus"];
            }
        }

        public int AllocatedWorkers
        {
            get
            {
                return _broker.AllocatedWorkers;
            }
        }

        public int BusyWorkers
        {
            get
            {
                return _broker.BusyWorkers;
            }
        }

        /// <summary>
        /// Publishes a new message to the specified event on the bus.
        /// </summary>
        /// <param name="source">A value representing the source of the data sent.</param>
        public virtual Task Publish(Message message)
        {
            Topic topic = _topics.GetOrAdd(message.Key, _ => new Topic(DefaultMessageStoreSize));

            topic.Store.Add(message);

            try
            {
                topic.SubscriptionLock.EnterReadLock();

                for (int i = 0; i < topic.Subscriptions.Count; i++)
                {
                    ISubscription subscription = topic.Subscriptions[i];
                    _broker.Schedule(subscription);
                }
            }
            finally
            {
                topic.SubscriptionLock.ExitReadLock();
            }

            _msgsTotalCounter.SafeIncrement();
            _msgsPerSecCounter.SafeIncrement();

            return TaskAsyncHelper.Empty;
        }

        protected ulong Save(Message message)
        {
            Topic topic = _topics.GetOrAdd(message.Key, _ => new Topic(DefaultMessageStoreSize));

            return topic.Store.Add(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public virtual IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            Subscription subscription = CreateSubscription(subscriber, cursor, callback, messageBufferSize);
            var topics = new HashSet<Topic>();

            foreach (var key in subscriber.EventKeys)
            {
                Topic topic = _topics.GetOrAdd(key, _ => new Topic(DefaultMessageStoreSize));

                // Set the subscription for this topic
                subscription.SetCursorTopic(key, topic);

                // Add it to the list of topics
                topics.Add(topic);
            }

            foreach (var topic in topics)
            {
                topic.AddSubscription(subscription);
            }

            if (!String.IsNullOrEmpty(cursor))
            {
                // Update all of the cursors so we're within the range
                foreach (var pair in subscription.Cursors)
                {
                    Topic topic;
                    if (_topics.TryGetValue(pair.Key, out topic) && pair.Id > topic.Store.GetMessageCount())
                    {
                        subscription.UpdateCursor(pair.Key, 0);
                    }
                }
            }

            Action<string> eventAdded = (eventKey) =>
            {
                Topic topic = _topics.GetOrAdd(eventKey, _ => new Topic(DefaultMessageStoreSize));

                ulong id = GetMessageId(eventKey);

                // Add or update the cursor (in case it already exists)
                subscription.AddOrUpdateCursor(eventKey, id, topic);

                // Add it to the list of subs
                topic.AddSubscription(subscription);
            };

            Action<string> eventRemoved = eventKey => RemoveEvent(subscription, eventKey);

            subscriber.EventAdded += eventAdded;
            subscriber.EventRemoved += eventRemoved;

            // If there's a cursor then schedule work for this subscription
            if (!String.IsNullOrEmpty(cursor))
            {
                _broker.Schedule(subscription);
            }

            return new DisposableAction(() =>
            {
                // This will stop work from continuting to happen
                subscription.Dispose();

                subscriber.EventAdded -= eventAdded;
                subscriber.EventRemoved -= eventRemoved;

                string currentCursor = Cursor.MakeCursor(subscription.Cursors);

                foreach (var eventKey in subscriber.EventKeys)
                {
                    RemoveEvent(subscription, eventKey);
                }

                subscription.Invoke(new MessageResult(currentCursor));
            });
        }

        private Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            IEnumerable<Cursor> cursors = null;
            if (cursor == null)
            {
                cursors = from key in subscriber.EventKeys
                          select new Cursor
                          {
                              Key = key,
                              Id = GetMessageId(key)
                          };
            }
            else
            {
                cursors = Cursor.GetCursors(cursor);
            }

            return new Subscription(subscriber.Identity, cursors, callback, messageBufferSize, _counters);
        }
        
        protected ulong GetMessageId(string key)
        {
            Topic topic;
            if (_topics.TryGetValue(key, out topic))
            {
                return topic.Store.GetMessageCount();
            }

            return 0;
        }

        public virtual void Dispose()
        {
        }

        private void RemoveEvent(Subscription subscription, string eventKey)
        {
            Topic topic;
            if (_topics.TryGetValue(eventKey, out topic))
            {
                topic.RemoveSubscription(subscription);
                subscription.RemoveCursor(eventKey);
            }
        }
    }
}
