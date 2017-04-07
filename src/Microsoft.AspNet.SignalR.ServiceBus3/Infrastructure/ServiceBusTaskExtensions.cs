// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus.Infrastructure
{
    public static class ServiceBusTaskExtensions
    {
        public static Task SendAsync(this TopicClient client, BrokeredMessage message)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            return client.SendAsync(message);
        }
    }
}
