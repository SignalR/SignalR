// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus.Infrastructure
{
    public static class ServiceBusTaskExtensions
    {
        public static Task SendAsync(this TopicClient client, BrokeredMessage message)
        {
            return Task.Factory.FromAsync((cb, state) => client.BeginSend((BrokeredMessage)state, cb, null), client.EndSend, message);
        }
    }
}
