// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.SignalR.Messaging;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Redis;
using System.Linq;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the Redis scale-out message bus implementation.
    /// </summary>
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        /// <summary>
        /// Constructor for single redis instance mode
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        /// <param name="password"></param>
        /// <param name="eventKey"></param>
        public RedisScaleoutConfiguration(string server, int port, string password, string eventKey)
            : this(CreateConnectionString(server, port, password), eventKey)
        {
        }

        /// <summary>
        /// Constructor for single redis instance mode
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="eventKey"></param>
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

            ConnectionStrings = new string[] { connectionString };
            if (connectionString.Length > 0)
            {
                var options = ConfigurationOptions.Parse(connectionString);
                Database = options.DefaultDatabase.GetValueOrDefault(0);
            }
            EventKey = eventKey;
        }

        /// <summary>
        /// Constructor for multi redis instance mode
        /// </summary>
        /// <param name="connectionStrings"></param>
        /// <param name="eventKey"></param>
        public RedisScaleoutConfiguration(string[] connectionStrings, string eventKey)
        {
            if (connectionStrings == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (eventKey == null)
            {
                throw new ArgumentNullException("eventKey");
            }

            ConnectionStrings = connectionStrings;

            EventKey = eventKey;
        }

        /// <summary>
        /// Constructor for multi redis instance mode
        /// </summary>
        /// <param name="endPoints"></param>
        /// <param name="eventKey"></param>
        public RedisScaleoutConfiguration(RedisEndPoint[] endPoints, string eventKey)
            : this(CreateConnectionStrings(endPoints), eventKey)
        {
        }


        /// <summary>
        /// The connection strings that needs to be passed to ConnectionMultiplexer
        /// Should be of the form server:port
        /// </summary>
        internal string[] ConnectionStrings { get; private set; }

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

        private static string[] CreateConnectionStrings(RedisEndPoint[] endPoints)
        {
            return endPoints.Select(e=> string.Format(CultureInfo.CurrentCulture, "{0}:{1}, password={2}, abortConnect=false", e.IpAddress, e.Port, e.Password)).ToArray();
        }
    }
}
