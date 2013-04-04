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
        /// Use Redis as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="server">The Redis server address.</param>
        /// <param name="port">The Redis server port.</param>
        /// <param name="password">The Redis server password.</param>
        /// <param name="eventKey">The Redis event key to use.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, string eventKey)
        {
            var configuration = new RedisScaleoutConfiguration(server, port, password, eventKey);

            return UseRedis(resolver, configuration);
        }

        /// <summary>
        /// Use Redis as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver</param>
        /// <param name="configuration">The Redis scale-out configuration options.</param> 
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
        {
            var bus = new Lazy<RedisMessageBus>(() => new RedisMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
