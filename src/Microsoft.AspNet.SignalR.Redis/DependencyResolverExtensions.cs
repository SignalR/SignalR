// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="server">The redis server.</param>
        /// <param name="port">The redis port.</param>
        /// <param name="password">The redis server password.</param>
        /// <param name="eventKey">The event keys.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, string eventKey)
        {
            var configuration = RedisScaleoutConfiguration.Create(server, port, password);
            configuration.EventKey = eventKey;

            return UseRedis(resolver, configuration);
        }

        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver</param>
        /// <param name="configuration">The scaleout configuration for redis.</param> 
        /// <returns></returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
        {
            var bus = new Lazy<RedisMessageBus>(() => new RedisMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
