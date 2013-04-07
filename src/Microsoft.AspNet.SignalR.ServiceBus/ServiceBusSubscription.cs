using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    internal class ServiceBusSubscription : IDisposable
    {
        private readonly NamespaceManager _namespaceManager;
        private readonly IList<SubscriptionContext> _subscriptions;
        private readonly IList<TopicClient> _clients;
        private readonly ServiceBusScaleoutConfiguration _configuration;

        public ServiceBusSubscription(ServiceBusScaleoutConfiguration configuration,
                                      NamespaceManager namespaceManager,
                                      IList<SubscriptionContext> subscriptions,
                                      IList<TopicClient> clients)
        {
            _configuration = configuration;
            _namespaceManager = namespaceManager;
            _subscriptions = subscriptions;
            _clients = clients;
        }

        public Task Publish(int topicIndex, Stream stream)
        {
            var message = new BrokeredMessage(stream, ownsStream: true)
            {
                TimeToLive = _configuration.TimeToLive
            };

            return _clients[topicIndex].SendAsync(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _clients.Count; i++)
                {
                    var subscription = _subscriptions[i];
                    subscription.Receiver.Close();

                    _clients[i].Close();

                    _namespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
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
