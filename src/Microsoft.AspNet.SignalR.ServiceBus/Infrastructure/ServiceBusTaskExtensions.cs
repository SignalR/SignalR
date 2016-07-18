// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
