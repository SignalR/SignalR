// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use the sErviceBus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="topicPrefix">The topic prefix to use. Typically represents the app name.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, string topicPrefix)
        {
            return UseServiceBus(resolver, connectionString, topicPrefix, 1);
        }

        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="topicPrefix">The topic prefix to use. Typically represents the app name.</param>
        /// <param name="numberOfTopics">The number of topics to use.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, string topicPrefix, int numberOfTopics)
        {
            var bus = new Lazy<ServiceBusMessageBus>(() => new ServiceBusMessageBus(connectionString, topicPrefix, numberOfTopics, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
