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
        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey)
            : this(CreateConnectionString(server, port, password), eventKey)
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

            var endpoint = new RedisScaleoutEndpoint();

            endpoint.ConnectionString = connectionString;
            if (connectionString.Length > 0)
            {
                var options = ConfigurationOptions.Parse(connectionString);
                endpoint.Database = options.DefaultDatabase.GetValueOrDefault(0);
            }
            endpoint.EventKey = eventKey;

            Endpoints = new[] { endpoint };
        }

        public RedisScaleoutConfiguration(RedisScaleoutEndpoint[] endpoints)
        {
            Endpoints = endpoints;
        }

        internal RedisScaleoutEndpoint[] Endpoints { get; }

        private static string CreateConnectionString(string server, int port, string password)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}:{1}, password={2}, abortConnect=false", server, port, password);
        }
    }
}
