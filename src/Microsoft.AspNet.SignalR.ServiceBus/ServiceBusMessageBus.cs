// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Messaging;
    using System;

    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        readonly TopicMessageBus bus;

        public ServiceBusMessageBus(string connectionString, int partitionCount, int nodeCount, int nodeId, string topicPrefix, IDependencyResolver resolver, TimeSpan messageDefaultTtl)
            : base(resolver)
        {
            this.bus = new TopicMessageBus(connectionString, partitionCount, nodeCount, nodeId, topicPrefix, messageDefaultTtl, OnReceived);
        }

        protected override Task Send(IList<Message> messages)
        {
            return this.bus.SendAsync(messages);
        }
    }
}
