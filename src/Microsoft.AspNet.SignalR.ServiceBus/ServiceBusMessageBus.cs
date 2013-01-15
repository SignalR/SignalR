// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Messaging;

    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        readonly TopicMessageBus bus;

        public ServiceBusMessageBus(string connectionString, int partitionCount, int nodeCount, int nodeId, string topicPrefix, IDependencyResolver resolver)
            : base(resolver)
        {
            this.bus = new TopicMessageBus(connectionString, partitionCount, nodeCount, nodeId, topicPrefix, OnReceived);
        }

        protected override Task Send(Message[] messages)
        {
            return this.bus.SendAsync(messages);
        }
    }
}
