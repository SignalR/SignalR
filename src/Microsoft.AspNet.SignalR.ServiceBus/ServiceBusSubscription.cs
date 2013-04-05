using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusSubscription : IDisposable
    {
        private readonly NamespaceManager _namespaceManager;
        private readonly List<SubscriptionContext> _subscriptions;
        private readonly ConcurrentDictionary<string, TopicClient> _clients;
        private readonly ServiceBusScaleoutConfiguration _configuration;

        public ServiceBusSubscription(ServiceBusScaleoutConfiguration configuration,
                                      NamespaceManager namespaceManager,
                                      List<SubscriptionContext> subscriptions, 
                                      ConcurrentDictionary<string, TopicClient> clients)
        {
            _configuration = configuration;
            _namespaceManager = namespaceManager;
            _subscriptions = subscriptions;
            _clients = clients;
        }

        public Task Publish(string topicName, Stream stream)
        {
            TopicClient client;
            if (_clients.TryGetValue(topicName, out client))
            {
                var message = new BrokeredMessage(stream, ownsStream: true)
                {
                    TimeToLive = _configuration.TimeToLive
                };

                return client.SendAsync(message);
            }

            // REVIEW: Should this return a faulted task?
            return TaskAsyncHelper.Empty;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Receiver.Close();

                _namespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);

                TopicClient client;
                if (_clients.TryRemove(subscription.TopicPath, out client))
                {
                    client.Close();
                }
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
}
