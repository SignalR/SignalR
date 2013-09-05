using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public object SubscriptionsLock { get; set; }
        public object TopicClientsLock { get; set; }

        public IList<string> TopicNames { get; set; }
        public Action<int, IEnumerable<BrokeredMessage>> Handler { get; set; }
        public Action<int, Exception> ErrorHandler { get; set; }

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
            var message = new BrokeredMessage(stream, ownsStream: true)
            {
                TimeToLive = _configuration.TimeToLive
            };

            return _topicClients[topicIndex].SendAsync(message);
        }

        public void UpdateSubscriptionContext(SubscriptionContext subscriptionContext, int topicIndex)
        {
            if (!IsDisposed)
            {
                _subscriptions[topicIndex] = subscriptionContext;
            }
        }

        public void UpdateTopicClients(TopicClient topicClient, int topicIndex)
        {
            if (!IsDisposed)
            {
                _topicClients[topicIndex] = topicClient;
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

    public class SubscriptionContext
    {
        public string TopicPath;
        public string Name;
        public MessageReceiver Receiver;

        public SubscriptionContext(string topicPath, string subName, MessageReceiver receiver)
        {
            TopicPath = topicPath;
            Name = subName;
            Receiver = receiver;
        }
    }
}
