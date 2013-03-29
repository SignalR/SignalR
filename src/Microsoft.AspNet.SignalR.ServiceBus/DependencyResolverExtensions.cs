// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use the ServiceBus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="topicPrefix">The topic prefix to use. Typically represents the app name.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, string topicPrefix)
        {
            var config = new ServiceBusScaleoutConfiguration
            {
                TopicCount = 1,
                TopicPrefix = topicPrefix,
                ConnectionString = connectionString
            };

            return UseServiceBus(resolver, config);
        }

        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The configuration to use.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration)
        {
            var bus = new Lazy<ServiceBusMessageBus>(() => new ServiceBusMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
