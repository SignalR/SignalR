using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalR
{
    internal class Topic
    {
        private HashSet<string> _subs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<Subscription> Subscriptions { get; private set; }
        public MessageStore<Message> Store { get; private set; }
        public ReaderWriterLockSlim SubscriptionLock { get; private set; }

        public Topic(uint storeSize)
        {
            Subscriptions = new List<Subscription>();
            Store = new MessageStore<Message>(storeSize);
            SubscriptionLock = new ReaderWriterLockSlim();
        }

        public void AddSubscription(Subscription subscription)
        {
            try
            {
                SubscriptionLock.EnterWriteLock();

                if (_subs.Add(subscription.Identity))
                {
                    Subscriptions.Add(subscription);
                }
            }
            finally
            {
                SubscriptionLock.ExitWriteLock();
            }
        }

        public void RemoveSubscription(Subscription subscription)
        {
            try
            {
                SubscriptionLock.EnterWriteLock();

                if (_subs.Remove(subscription.Identity))
                {
                    Subscriptions.Remove(subscription);
                }
            }
            finally
            {
                SubscriptionLock.ExitWriteLock();
            }
        }
    }
}
