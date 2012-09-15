using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalR
{
    public class Topic
    {
        private HashSet<string> _subcriptionIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<ISubscription> Subscriptions { get; private set; }
        public MessageStore<Message> Store { get; private set; }
        public ReaderWriterLockSlim SubscriptionLock { get; private set; }

        public Topic(uint storeSize)
        {
            Subscriptions = new List<ISubscription>();
            Store = new MessageStore<Message>(storeSize);
            SubscriptionLock = new ReaderWriterLockSlim();
        }

        public void AddSubscription(ISubscription subscription)
        {
            try
            {
                SubscriptionLock.EnterWriteLock();

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

                if (_subcriptionIdentities.Remove(subscription.Identity))
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
