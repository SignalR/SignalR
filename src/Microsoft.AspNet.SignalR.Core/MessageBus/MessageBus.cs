﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : IMessageBus, IDisposable
    {
        protected readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>();
        private readonly MessageBroker _broker;

        private const int DefaultMessageStoreSize = 5000;

        private readonly IStringMinifier _stringMinifier;

        private readonly ITraceManager _trace;

        protected readonly IPerformanceCounterManager _counters;

        private Timer _gcTimer;
        private int _gcRunning;
        private static readonly TimeSpan _gcInterval = TimeSpan.FromSeconds(15);

        private readonly TimeSpan _topicTtl;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<IStringMinifier>(),
                   resolver.Resolve<ITraceManager>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<IConfigurationManager>())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceManager"></param>
        /// <param name="performanceCounterManager"></param>
        public MessageBus(IStringMinifier stringMinifier, ITraceManager traceManager, IPerformanceCounterManager performanceCounterManager, IConfigurationManager configurationManager)
        {
            _stringMinifier = stringMinifier;
            _trace = traceManager;
            _counters = performanceCounterManager;

            _gcTimer = new Timer(_ => CheckTopics(), state: null, dueTime: _gcInterval, period: _gcInterval);

            _broker = new MessageBroker(_counters)
            {
                Trace = Trace
            };

            // Keep topics alive for as long as we let connections wait until they are disconnected.
            // This should be a good enough estimate for how long until we should consider a topic dead.
            _topicTtl = configurationManager.DisconnectTimeout;
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
        /// <param name="message">The message to publish.</param>
        public virtual Task Publish(Message message)
        {
            Topic topic = GetTopic(message.Key);

            topic.Store.Add(message);

            _counters.MessageBusMessagesPublishedTotal.Increment();
            _counters.MessageBusMessagesPublishedPerSec.Increment();

            ScheduleTopic(topic);

            return TaskAsyncHelper.Empty;
        }

        protected ulong Save(Message message)
        {
            Topic topic = GetTopic(message.Key);

            ulong id = topic.Store.Add(message);

            _counters.MessageBusMessagesPublishedTotal.Increment();
            _counters.MessageBusMessagesPublishedPerSec.Increment();

            return id;
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
                Topic topic = GetTopic(key);

                // Set the subscription for this topic
                subscription.SetEventTopic(key, topic);

                topics.Add(topic);
            }

            Action<string> eventAdded = eventKey =>
            {
                Topic topic = GetTopic(eventKey);

                // Add or update the cursor (in case it already exists)
                subscription.AddEvent(eventKey, topic);

                // Add it to the list of subs
                topic.AddSubscription(subscription);
            };

            Action<string> eventRemoved = eventKey => RemoveEvent(subscription, eventKey);

            subscriber.EventAdded += eventAdded;
            subscriber.EventRemoved += eventRemoved;

            // Add the subscription when it's all set and can be scheduled
            // for work
            foreach (var topic in topics)
            {
                topic.AddSubscription(subscription);
            }

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

                string currentCursor = subscription.GetCursor();

                foreach (var eventKey in subscriber.EventKeys)
                {
                    RemoveEvent(subscription, eventKey);
                }

                subscription.Invoke(new MessageResult(currentCursor));
            });
        }

        protected virtual Subscription CreateSubscription(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            return new DefaultSubscription(subscriber.Identity, subscriber.EventKeys, _topics, cursor, callback, messageBufferSize, _stringMinifier, _counters);
        }

        protected void ScheduleEvent(string eventKey)
        {
            Topic topic;
            if (_topics.TryGetValue(eventKey, out topic))
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

        public virtual void Dispose()
        {
            // Stop the broker from doing any work
            _broker.Dispose();

            // Spin while we wait for the timer to finish if it's currently running
            while (Interlocked.Exchange(ref _gcRunning, 1) == 1)
            {
                Thread.Sleep(250);
            }

            // Remove all topics
            _topics.Clear();

            if (_gcTimer != null)
            {
                _gcTimer.Dispose();
            }
        }

        private void CheckTopics()
        {
            if (Interlocked.Exchange(ref _gcRunning, 1) == 1)
            {
                return;
            }

            foreach (var pair in _topics)
            {
                if (pair.Value.IsExpired)
                {
                    // Only remove the topic if it's expired and we changed the state to dead
                    if (Interlocked.CompareExchange(ref pair.Value.State,
                                                    Topic.TopicState.Dead,
                                                    Topic.TopicState.NoSubscriptions) == Topic.TopicState.NoSubscriptions)
                    {
                        Topic topic;
                        _topics.TryRemove(pair.Key, out topic);
                        _stringMinifier.RemoveUnminified(pair.Key);
                    }
                }
            }

            Interlocked.Exchange(ref _gcRunning, 0);
        }

        private Topic GetTopic(string key)
        {
            Func<string, Topic> factory = _ => new Topic(DefaultMessageStoreSize, _topicTtl);

            while (true)
            {
                Topic topic = _topics.GetOrAdd(key, factory);

                // If we sucessfully marked it as active then bail
                if (Interlocked.CompareExchange(ref topic.State,
                                                Topic.TopicState.Active,
                                                Topic.TopicState.NoSubscriptions) != Topic.TopicState.Dead)
                {
                    return topic;
                }
            }
        }

        private void RemoveEvent(Subscription subscription, string eventKey)
        {
            Topic topic;
            if (_topics.TryGetValue(eventKey, out topic))
            {
                topic.RemoveSubscription(subscription);
                subscription.RemoveEvent(eventKey);
            }
        }
    }
}
