// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use Windows Azure Service Bus as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The Service Bus connection string to use.</param>
        /// <param name="topicPrefix">The topic prefix to use. Typically represents the app name. This must be consistent between all nodes in the web farm.</param>
        /// <returns>The dependency resolver</returns>
        /// <remarks>Note: Only Windows Azure Service Bus is supported. Service Bus for Windows Server (on-premise) is not supported.</remarks>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, string topicPrefix)
        {
            var config = new ServiceBusScaleoutConfiguration(connectionString, topicPrefix);

            return UseServiceBus(resolver, config);
        }

        /// <summary>
        /// Use Windows Azure Service Bus as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The Service Bus scale-out configuration options.</param>
        /// <returns>The dependency resolver</returns>
        /// <remarks>Note: Only Windows Azure Service Bus is supported. Service Bus for Windows Server (on-premise) is not supported.</remarks>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration)
        {
            var bus = new ServiceBusMessageBus(resolver, configuration);
            resolver.Register(typeof(IMessageBus), () => bus);

            return resolver;
        }
    }
}
