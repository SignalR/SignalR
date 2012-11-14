// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly MessageBroker _broker;

        private const int DefaultMessageStoreSize = 5000;

        private readonly IStringMinifier _stringMinifier;

        private readonly ITraceManager _traceManager;
        private readonly TraceSource _trace;

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
        /// <param name="stringMinifier"></param>
        /// <param name="traceManager"></param>
        /// <param name="performanceCounterManager"></param>
        /// <param name="configurationManager"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The message broker is disposed when the bus is disposed.")]
        public MessageBus(IStringMinifier stringMinifier,
                          ITraceManager traceManager,
                          IPerformanceCounterManager performanceCounterManager,
                          IConfigurationManager configurationManager)
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

            _stringMinifier = stringMinifier;
            _traceManager = traceManager;
            Counters = performanceCounterManager;
            _trace = _traceManager["SignalR.MessageBus"];

            _gcTimer = new Timer(_ => CheckTopics(), state: null, dueTime: _gcInterval, period: _gcInterval);

            _broker = new MessageBroker(Counters)
            {
                Trace = Trace
            };

            // Keep topics alive for as long as we let connections wait until they are disconnected.
            // This should be a good enough estimate for how long until we should consider a topic dead.
            _topicTtl = configurationManager.DisconnectTimeout;

            Topics = new ConcurrentDictionary<string, Topic>();
        }

        private TraceSource Trace
        {
            get
            {
                return _trace;
            }
        }

        protected ConcurrentDictionary<string, Topic> Topics { get; private set; }
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

            Topic topic = GetTopic(message.Key);

            topic.Store.Add(message);

            Counters.MessageBusMessagesPublishedTotal.Increment();
            Counters.MessageBusMessagesPublishedPerSec.Increment();

            ScheduleTopic(topic);

            return TaskAsyncHelper.Empty;
        }

        protected ulong Save(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

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
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Failure to invoke the callback should be ignored")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable object is returned to the caller")]
        public virtual IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int maxMessages)
        {
            Subscription subscription = CreateSubscription(subscriber, cursor, callback, maxMessages);

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

            subscriber.EventKeyAdded += eventAdded;
            subscriber.EventKeyRemoved += eventRemoved;
            subscriber.GetCursor += subscription.GetCursor;

            // Add the subscription when it's all set and can be scheduled
            // for work
            foreach (var topic in topics)
            {
                topic.AddSubscription(subscription);
            }

            var disposable = new DisposableAction(() =>
            {
                // This will stop work from continuting to happen
                subscription.Dispose();

                try
                {
                    // Invoke the terminal callback
                    subscription.Invoke(MessageResult.TerminalMessage).Wait();
                }
                catch
                {
                    // We failed to talk to the subscriber because they are already gone
                    // so the terminal message isn't required.
                }

                subscriber.EventKeyAdded -= eventAdded;
                subscriber.EventKeyRemoved -= eventRemoved;
                subscriber.GetCursor -= subscription.GetCursor;

                foreach (var eventKey in subscriber.EventKeys)
                {
                    RemoveEvent(subscription, eventKey);
                }
            });

            // When the subscription itself is disposed then dispose it
            subscription.DisposedCallback = disposable.Dispose;

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

        private void CheckTopics()
        {
            if (Interlocked.Exchange(ref _gcRunning, 1) == 1)
            {
                return;
            }

            foreach (var pair in Topics)
            {
                if (pair.Value.IsExpired)
                {
                    // Only remove the topic if it's expired and we changed the state to dead
                    if (Interlocked.CompareExchange(ref pair.Value.State,
                                                    Topic.TopicState.Dead,
                                                    Topic.TopicState.NoSubscriptions) == Topic.TopicState.NoSubscriptions)
                    {
                        Topic topic;
                        Topics.TryRemove(pair.Key, out topic);
                        _stringMinifier.RemoveUnminified(pair.Key);

                        Trace.TraceInformation("RemoveTopic(" + pair.Key + ")");
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
                Topic topic = Topics.GetOrAdd(key, factory);

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
            if (Topics.TryGetValue(eventKey, out topic))
            {
                topic.RemoveSubscription(subscription);
                subscription.RemoveEvent(eventKey);
            }
        }
    }
}
