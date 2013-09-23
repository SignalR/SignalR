using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusConnectionContext : IDisposable
    {
        private readonly NamespaceManager _namespaceManager;
        private readonly ServiceBusScaleoutConfiguration _configuration;

        private readonly SubscriptionContext[] _subscriptions;
        private readonly TopicClient[] _topicClients;

        public object SubscriptionsLock { get; private set; }
        public object TopicClientsLock { get; private set; }

        public IList<string> TopicNames { get; private set; }
        public Action<int, IEnumerable<BrokeredMessage>> Handler { get; private set; }
        public Action<int, Exception> ErrorHandler { get; private set; }

        public bool IsDisposed { get; private set; }

        public ServiceBusConnectionContext(ServiceBusScaleoutConfiguration configuration,
                                           NamespaceManager namespaceManager,
                                           IList<string> topicNames,
                                           Action<int, IEnumerable<BrokeredMessage>> handler,
                                           Action<int, Exception> errorHandler)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException("topicNames");
            }

            _namespaceManager = namespaceManager;
            _configuration = configuration;

            _subscriptions = new SubscriptionContext[topicNames.Count];
            _topicClients = new TopicClient[topicNames.Count];

            TopicNames = topicNames;
            Handler = handler;
            ErrorHandler = errorHandler;

            TopicClientsLock = new object();
            SubscriptionsLock = new object();
        }

        public Task Publish(int topicIndex, Stream stream)
        {
            if (IsDisposed)
            {
                return TaskAsyncHelper.Empty;
            }

            var message = new BrokeredMessage(stream, ownsStream: true)
            {
                TimeToLive = _configuration.TimeToLive
            };

            return _topicClients[topicIndex].SendAsync(message);
        }

        internal void SetSubscriptionContext(SubscriptionContext subscriptionContext, int topicIndex)
        {
            lock (SubscriptionsLock)
            {
                if (!IsDisposed)
                {
                    _subscriptions[topicIndex] = subscriptionContext;
                }
            }
        }

        internal void SetTopicClients(TopicClient topicClient, int topicIndex)
        {
            lock (TopicClientsLock)
            {
                if (!IsDisposed)
                {
                    _topicClients[topicIndex] = topicClient;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed)
                {
                    lock (TopicClientsLock)
                    {
                        lock (SubscriptionsLock)
                        {
                            for (int i = 0; i < TopicNames.Count; i++)
                            {
                                _topicClients[i].Close();
                                SubscriptionContext subscription = _subscriptions[i];
                                subscription.Receiver.Close();
                                _namespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);
                            }

                            IsDisposed = true;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}