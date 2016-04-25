using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusFactory
    {
        private readonly string _connectionString;
        readonly NamespaceManager _namespaceManager;

        public ServiceBusFactory(string connectionString)
        {
            _connectionString = connectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
        }

        public SubscriptionClient GetTopicClient(SubscriptionDescription subscription, ReceiveMode mode = ReceiveMode.PeekLock)
        {
            return SubscriptionClient.CreateFromConnectionString(_connectionString, subscription.TopicPath, subscription.Name, mode);
        }

        public TopicClient GetTopic(TopicDescription topic)
        {
            return TopicClient.CreateFromConnectionString(_connectionString, topic.Path);
        }

        public TopicDescription CreateIfNotExists(TopicDescription topic)
        {
            return _namespaceManager.TopicExists(topic.Path) ?
                   topic : _namespaceManager.CreateTopic(topic);
        }

        public SubscriptionDescription CreateIfNotExists(SubscriptionDescription subscription)
        {
            return _namespaceManager.SubscriptionExists(subscription.TopicPath, subscription.Name) ?
                   subscription : _namespaceManager.CreateSubscription(subscription);
        }
    }
}
