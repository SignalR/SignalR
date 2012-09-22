using System;
using System.Collections.Generic;
using SignalR.Infrastructure;

namespace SignalR.ServiceBus
{
    public static class DependencyResolverExtensions
    {        
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, int partitionCount, int nodeCount, int nodeId, string topicPrefix)
        {
            var bus = new Lazy<ServiceBusMessageBus>(() => new ServiceBusMessageBus(connectionString, partitionCount, nodeCount, nodeId, topicPrefix, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
