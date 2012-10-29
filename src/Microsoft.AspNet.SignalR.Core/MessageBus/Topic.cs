// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.SignalR
{
    public class Topic
    {
        private readonly HashSet<string> _subcriptionIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _ttl;

        // Keeps track of the last time this subscription was used
        private DateTime _lastUsed = DateTime.UtcNow;

        public IList<ISubscription> Subscriptions { get; private set; }
        public MessageStore<Message> Store { get; private set; }
        public ReaderWriterLockSlim SubscriptionLock { get; private set; }

        // State of the topic
        internal int State;

        public bool IsExpired
        {
            get
            {
                try
                {
                    SubscriptionLock.EnterReadLock();

                    return Subscriptions.Count == 0 && (DateTime.UtcNow - _lastUsed) > _ttl;
                }
                finally
                {
                    SubscriptionLock.ExitReadLock();
                }
            }
        }

        public Topic(uint storeSize, TimeSpan ttl)
        {
            _ttl = ttl;
            Subscriptions = new List<ISubscription>();
            Store = new MessageStore<Message>(storeSize);
            SubscriptionLock = new ReaderWriterLockSlim();
        }

        public void AddSubscription(ISubscription subscription)
        {
            try
            {
                SubscriptionLock.EnterWriteLock();

                _lastUsed = DateTime.UtcNow;

                if (_subcriptionIdentities.Add(subscription.Identity))
                {
                    Subscriptions.Add(subscription);
                }
            }
            finally
            {
                SubscriptionLock.ExitWriteLock();
            }
        }

        public void RemoveSubscription(ISubscription subscription)
        {
            try
            {
                SubscriptionLock.EnterWriteLock();

                _lastUsed = DateTime.UtcNow;

                if (_subcriptionIdentities.Remove(subscription.Identity))
                {
                    Subscriptions.Remove(subscription);
                }

                if (Subscriptions.Count == 0)
                {
                    // Change the state from active -> no subs
                    Interlocked.CompareExchange(ref State, TopicState.NoSubscriptions, TopicState.Active);
                }
            }
            finally
            {
                SubscriptionLock.ExitWriteLock();
            }
        }

        internal class TopicState
        {
            public static int NoSubscriptions = 0;
            public static int Active = 1;
            public static int Dead = 2;
        }
    }
}
