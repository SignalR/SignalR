// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNet.SignalR
{
    public class RedisScaleoutEndpoint
    {
        /// <summary>
        /// The connection string that needs to be passed to ConnectionMultiplexer
        /// Should be of the form server:port
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The Redis database instance to use.
        /// Defaults to 0.
        /// </summary>
        public int Database { get; set; }

        /// <summary>
        /// The Redis event key to use.
        /// </summary>
        public string EventKey { get; set; }
    }
}
