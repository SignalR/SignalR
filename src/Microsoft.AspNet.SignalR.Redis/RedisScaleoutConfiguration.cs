// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.SignalR.Messaging;
using StackExchange.Redis;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the Redis scale-out message bus implementation.
    /// </summary>
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey, bool publishOnly = false)
            : this(CreateConnectionString(server, port, password), eventKey)
        {
            PublishOnly = publishOnly;
        }

        public RedisScaleoutConfiguration(string connectionString, string eventKey, bool publishOnly=false)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (eventKey == null)
            {
                throw new ArgumentNullException("eventKey");
            }

            ConnectionString = connectionString;
            EventKey = eventKey;
            PublishOnly = publishOnly;
        }

        /// <summary>
        /// The connection string that needs to be passed to ConnectionMultiplexer
        /// Should be of the form server:port
        /// </summary>
        internal string ConnectionString { get; private set; }

        /// <summary>
        /// The Redis database instance to use.
        /// Defaults to 0.
        /// </summary>
        public int Database { get; set; }

        /// <summary>
        /// If true, this instance will only publish to Redis but not subscribe to events
        /// </summary>
        public bool PublishOnly { get; set; }

        /// <summary>
        /// The Redis event key to use.
        /// </summary>
        public string EventKey { get; private set; }

        private static string CreateConnectionString(string server, int port, string password)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}:{1}, password={2}, abortConnect=false", server, port, password);
        }
    }
}
