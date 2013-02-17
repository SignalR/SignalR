// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : IMessageBus, IDisposable
    {
        private readonly MessageBroker _broker;

        // The size of the messages store we allocate per topic.
        private readonly uint _messageStoreSize;

        // By default, topics are cleaned up after having no subscribers and after 
        // an interval based on the disconnect timeout has passed. While this works in normal cases
        // it's an issue when the rate of incoming connections is too high. 
        // This is the maximum number of un-expired topics with no subscribers 
        // we'll leave hanging around. The rest will be cleaned up on an the gc interval.
        private readonly int _maxTopicsWithNoSubscriptions;

        private readonly IStringMinifier _stringMinifier;

        private readonly ITraceManager _traceManager;
        private readonly TraceSource _trace;

        private Timer _gcTimer;
        private int _gcRunning;
        private static readonly TimeSpan _gcInterval = TimeSpan.FromSeconds(15);

        private readonly TimeSpan _topicTtl;

        // For unit testing
        internal Action<string, Topic> BeforeTopicGarbageCollected;
        internal Action<string, Topic> AfterTopicGarbageCollected;
        internal Action<string, Topic> BeforeTopicMarked;
        internal Action<string> BeforeTopicCreated;
        internal Action<string, Topic> AfterTopicMarkedSuccessfully;
        internal Action<string, Topic, int> AfterTopicMarked;

        private const int DefaultMaxTopicsWithNoSubscriptions = 5000;

        private readonly Func<string, Topic> _createTopic;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<IStringMinifier>(),
                   resolver.Resolve<ITraceManager>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<IConfigurationManager>(),
                   DefaultMaxTopicsWithNoSubscriptions)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringMinifier"></param>
        /// <param name="traceManager"></param>
        /// <param name="performanceCounterManager"></param>
        /// <param name="configurationManager"></param>
        /// <param name="maxTopicsWithNoSubscriptions"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The message broker is disposed when the bus is disposed.")]
        public MessageBus(IStringMinifier stringMinifier,
                          ITraceManager traceManager,
                          IPerformanceCounterManager performanceCounterManager,
                          IConfigurationManager configurationManager,
                          int maxTopicsWithNoSubscriptions)
        {
            if (stringMinifier == null)
            {
                throw new ArgumentNullException("stringMinifier");
            }

            if (traceManager == null)
            {
                throw new ArgumentNullException("traceManager");
            }

            if (performanceCounterManager == null)
            {
                throw new ArgumentNullException("performanceCounterManager");
            }

            if (configurationManager == null)
            {
                throw new ArgumentNullException("configurationManager");
            }

            if (configurationManager.DefaultMessageBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(Resources.Error_BufferSizeOutOfRange);
            }

            _stringMinifier = stringMinifier;
            _traceManager = traceManager;
            Counters = performanceCounterManager;
            _trace = _traceManager["SignalR.MessageBus"];
            _maxTopicsWithNoSubscriptions = maxTopicsWithNoSubscriptions;

            _gcTimer = new Timer(_ => GarbageCollectTopics(), state: null, dueTime: _gcInterval, period: _gcInterval);

            _broker = new MessageBroker(Counters)
            {
                Trace = Trace
            };

            // The default message store size
            _messageStoreSize = (uint)configurationManager.DefaultMessageBufferSize;

            _topicTtl = configurationManager.TopicTtl();
            _createTopic = CreateTopic;

            Topics = new TopicLookup();
        }

        private TraceSource Trace
        {
            get
            {
                return _trace;
            }
        }

        protected internal TopicLookup Topics { get; private set; }
        protected IPerformanceCounterManager Counters { get; private set; }

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
        /// <param name="message">The message to publish.</param>
        public virtual Task Publish(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            Topic topic;
            if (Topics.TryGetValue(message.Key, out topic))
            {
                topic.Store.Add(message);
                ScheduleTopic(topic);
            }

            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();


            return TaskAsyncHelper.Empty;
        }

        protected ulong Save(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            // Don't mark topics as active when publishing
            Topic topic = GetTopic(message.Key);

            ulong id = topic.Store.Add(message);

            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();

            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable object is returned to the caller")]
        public virtual IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int maxMessages)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException("subscriber");
            }

            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            Subscription subscription = CreateSubscription(subscriber, cursor, callback, maxMessages);

            // Set the subscription for this subscriber
            subscriber.Subscription = subscription;

            var topics = new HashSet<Topic>();

            foreach (var key in subscriber.EventKeys)
            {
                Topic topic = GetTopic(key);

                // Set the subscription for this topic
                subscription.SetEventTopic(key, topic);

                topics.Add(topic);
            }

            subscriber.EventKeyAdded += AddEvent;
            subscriber.EventKeyRemoved += RemoveEvent;
            subscriber.GetCursor = subscription.GetCursor;

            // Add the subscription when it's all set and can be scheduled
            // for work
            foreach (var topic in topics)
            {
                topic.AddSubscription(subscription);
            }

            var disposable = new DisposableAction(DisposeSubscription, subscriber);

            // When the subscription itself is disposed then dispose it
            subscription.Disposable = disposable;

            // If there's a cursor then schedule work for this subscription
            if (!String.IsNullOrEmpty(cursor))
            {
                _broker.Schedule(subscription);
            }

            return disposable;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Called from derived class")]
        protected virtual Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            return new DefaultSubscription(subscriber.Identity, subscriber.EventKeys, Topics, cursor, callback, messageBufferSize, _stringMinifier, Counters);
        }

        protected void ScheduleEvent(string eventKey)
        {
            Topic topic;
            if (Topics.TryGetValue(eventKey, out topic))
            {
                ScheduleTopic(topic);
            }
        }

        private void ScheduleTopic(Topic topic)
        {
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
        }

        /// <summary>
        /// Creates a topic for the specified key.
        /// </summary>
        /// <param name="key">The key to create the topic for.</param>
        /// <returns>A <see cref="Topic"/> for the specified key.</returns>
        protected virtual Topic CreateTopic(string key)
        {
            // REVIEW: This can be called multiple times, should we guard against it?
            Counters.MessageBusTopicsCurrent.Increment();

            return new Topic(_messageStoreSize, _topicTtl);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop the broker from doing any work
                _broker.Dispose();

                // Spin while we wait for the timer to finish if it's currently running
                while (Interlocked.Exchange(ref _gcRunning, 1) == 1)
                {
                    Thread.Sleep(250);
                }

                // Remove all topics
                Topics.Clear();

                if (_gcTimer != null)
                {
                    _gcTimer.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        internal void GarbageCollectTopics()
        {
            if (Interlocked.Exchange(ref _gcRunning, 1) == 1)
            {
                return;
            }

            int topicsWithNoSubs = 0;

            foreach (var pair in Topics)
            {
                if (pair.Value.IsExpired)
                {
                    if (BeforeTopicGarbageCollected != null)
                    {
                        BeforeTopicGarbageCollected(pair.Key, pair.Value);
                    }

                    // Mark the topic as dead
                    DestroyTopic(pair.Key, pair.Value);
                }
                else if (pair.Value.State == TopicState.NoSubscriptions)
                {
                    // Keep track of the number of topics with no subscriptions
                    topicsWithNoSubs++;
                }
            }

            int overflow = topicsWithNoSubs - _maxTopicsWithNoSubscriptions;
            if (overflow > 0)
            {
                // If we've overflowed the max the collect topics that don't have
                // subscribers
                var candidates = new List<KeyValuePair<string, Topic>>();
                foreach (var pair in Topics)
                {
                    if (pair.Value.State == TopicState.NoSubscriptions)
                    {
                        candidates.Add(pair);
                    }
                }

                // We want to remove the overflow but oldest first
                candidates.Sort((leftPair, rightPair) => rightPair.Value.LastUsed.CompareTo(leftPair.Value.LastUsed));

                // Clear up to the overflow and stay within bounds
                for (int i = 0; i < overflow && i < candidates.Count; i++)
                {
                    var pair = candidates[i];

                    // Mark it as dead
                    Interlocked.Exchange(ref pair.Value.State, TopicState.Dead);

                    // Kill it
                    DestroyTopicCore(pair.Key, pair.Value);
                }
            }

            Interlocked.Exchange(ref _gcRunning, 0);
        }

        private void DestroyTopic(string key, Topic topic)
        {
            var state = Interlocked.Exchange(ref topic.State, TopicState.Dead);

            switch (state)
            {
                case TopicState.NoSubscriptions:
                    DestroyTopicCore(key, topic);
                    break;
                default:
                    // Restore the old state
                    Interlocked.Exchange(ref topic.State, state);
                    break;
            }
        }

        private void DestroyTopicCore(string key, Topic topic)
        {
            Topics.TryRemove(key);
            _stringMinifier.RemoveUnminified(key);

            Counters.MessageBusTopicsCurrent.Decrement();

            Trace.TraceInformation("RemoveTopic(" + key + ")");

            if (AfterTopicGarbageCollected != null)
            {
                AfterTopicGarbageCollected(key, topic);
            }
        }

        internal Topic GetTopic(string key)
        {
            while (true)
            {
                if (BeforeTopicCreated != null)
                {
                    BeforeTopicCreated(key);
                }

                Topic topic = Topics.GetOrAdd(key, _createTopic);

                if (BeforeTopicMarked != null)
                {
                    BeforeTopicMarked(key, topic);
                }

                var oldState = Interlocked.Exchange(ref topic.State, TopicState.HasSubscriptions);

                if (AfterTopicMarked != null)
                {
                    AfterTopicMarked(key, topic, oldState);
                }

                if (oldState != TopicState.Dead)
                {
                    if (AfterTopicMarkedSuccessfully != null)
                    {
                        AfterTopicMarkedSuccessfully(key, topic);
                    }

                    return topic;
                }
            }
        }

        private void AddEvent(ISubscriber subscriber, string eventKey)
        {
            Topic topic = GetTopic(eventKey);

            // Add or update the cursor (in case it already exists)
            subscriber.Subscription.AddEvent(eventKey, topic);

            // Add it to the list of subs
            topic.AddSubscription(subscriber.Subscription);
        }

        private void RemoveEvent(ISubscriber subscriber, string eventKey)
        {
            Topic topic;
            if (Topics.TryGetValue(eventKey, out topic))
            {
                topic.RemoveSubscription(subscriber.Subscription);
                subscriber.Subscription.RemoveEvent(eventKey);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Failure to invoke the callback should be ignored")]
        private void DisposeSubscription(object state)
        {
            var subscriber = (ISubscriber)state;

            // This will stop work from continuting to happen
            subscriber.Subscription.Dispose();

            try
            {
                // Invoke the terminal callback
                subscriber.Subscription.Invoke(MessageResult.TerminalMessage).Wait();
            }
            catch
            {
                // We failed to talk to the subscriber because they are already gone
                // so the terminal message isn't required.
            }

            subscriber.EventKeyAdded -= AddEvent;
            subscriber.EventKeyRemoved -= RemoveEvent;
            subscriber.GetCursor = null;

            foreach (var eventKey in subscriber.EventKeys)
            {
                RemoveEvent(subscriber, eventKey);
            }
        }
    }
}
