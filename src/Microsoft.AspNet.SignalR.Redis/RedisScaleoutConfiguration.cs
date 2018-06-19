﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the Redis scale-out message bus implementation.
    /// </summary>
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey)
            : this(CreateConnectionString(server, port, password), eventKey)
        {
        }

        public RedisScaleoutConfiguration(IEnumerable<RedisEndPoint> endPoints, string password, string eventKey)
    : this(CreateConnectionString(endPoints, password), eventKey)
        {
        }

        public RedisScaleoutConfiguration(string connectionString, string eventKey)
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
            if (connectionString.Length > 0)
            {
                var options = ConfigurationOptions.Parse(connectionString);
                Database = options.DefaultDatabase.GetValueOrDefault(0);
            }
            EventKey = eventKey;
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
        /// The Redis event key to use.
        /// </summary>
        public string EventKey { get; private set; }

        private static string CreateConnectionString(string server, int port, string password)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}:{1}, password={2}, abortConnect=false", server, port, password);
        }

        private static string CreateConnectionString(IEnumerable<RedisEndPoint> endPoints, string password)
        {
            var endPointList = string.Join(",", endPoints.Select(x => x.IpAddress + ":" + x.Port));
            return string.Format(CultureInfo.CurrentCulture, "{0}, password={1}, abortConnect=false", endPointList, password);
        }
    }
}
