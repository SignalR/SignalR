// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the Redis scale-out message bus implementation.
    /// </summary>
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey)
            : this(MakeConnectionFactory(server, port, password), eventKey)
        {

        }

        public RedisScaleoutConfiguration(Func<RedisConnection> connectionFactory, string eventKey)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            if (eventKey == null)
            {
                throw new ArgumentNullException("eventKey");
            }

            ConnectionFactory = connectionFactory;
            EventKey = eventKey;
        }

        internal Func<RedisConnection> ConnectionFactory { get; private set; }

        /// <summary>
        /// The Redis database instance to use.
        /// Defaults to 0.
        /// </summary>
        public int Database { get; set; }

        /// <summary>
        /// The Redis event key to use.
        /// </summary>
        public string EventKey { get; private set; }

        private static Func<RedisConnection> MakeConnectionFactory(string server, int port, string password)
        {
            return () => new RedisConnection(server, port: port, password: password);
        }
    }
}
