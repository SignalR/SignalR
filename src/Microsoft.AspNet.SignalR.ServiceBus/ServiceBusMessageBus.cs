namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Threading.Tasks;

    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        const bool IsAnonymous = false;

        readonly TopicMessageBus bus;
        readonly string topicPrefix;

        public ServiceBusMessageBus(string connectionString, int partitionCount, int nodeCount, int nodeId, string topicPrefix, IDependencyResolver resolver)
            : base(resolver)
        {
            this.bus = new TopicMessageBus(connectionString, partitionCount, nodeCount, nodeId, topicPrefix, IsAnonymous, OnReceived);
        }

        protected override Task Send(Message[] messages)
        {
            return this.bus.SendAsync(messages);
        }
    }
}
