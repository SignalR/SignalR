// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.EventHub;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use Windows Azure Event Hub as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="eventHubConnectionString">The Event Hub service bus connection string to use.</param>
        /// <param name="storageConnectionString">The Event Hub storage connection string to use.</param>
        /// <param name="eventHubName">The event hub prefix to use. Typically represents the app name. This must be consistent between all nodes in the web farm.</param>
        /// <param name="consumerGroupName">Use env. machine name.</param>
        /// <returns>The dependency resolver</returns>
        /// <remarks>Note: Only Windows Azure Event Hub is supported. Event Hub for Windows Server (on-premise) is not supported.</remarks>
        public static IDependencyResolver UseEventHub(this IDependencyResolver resolver, string eventHubConnectionString, string storageConnectionString, string eventHubName, string consumerGroupName)
        {
            var config = new EventHubScaleoutConfiguration(eventHubConnectionString, storageConnectionString, eventHubName, consumerGroupName);
            return UseEventHub(resolver, config);
        }

        /// <summary>
        /// Use Windows Azure Event Hub as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The Event Hub scale-out configuration options.</param>
        /// <returns>The dependency resolver</returns>
        /// <remarks>Note: Only Windows Azure Event Hub is supported. Event Hub for Windows Server (on-premise) is not supported.</remarks>
        public static IDependencyResolver UseEventHub(this IDependencyResolver resolver, EventHubScaleoutConfiguration configuration)
        {
            var bus = new EventHubMessageBus(resolver, configuration);
            resolver.Register(typeof(IMessageBus), () => bus);
            return resolver;
        }
    }
}
