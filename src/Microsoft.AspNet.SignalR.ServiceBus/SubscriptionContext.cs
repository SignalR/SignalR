// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    internal class SubscriptionContext
    {
        public string TopicPath { get; private set; }
        public string Name { get; private set; }
        public MessageReceiver Receiver { get; private set; }

        public SubscriptionContext(string topicPath, string subName, MessageReceiver receiver)
        {
            TopicPath = topicPath;
            Name = subName;
            Receiver = receiver;
        }
    }
}
