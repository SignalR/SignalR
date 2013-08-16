// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNet.SignalR.Redis
{
    [Serializable]
    public class RedisConnectionClosedException : Exception
    {
        public RedisConnectionClosedException() { }
        public RedisConnectionClosedException(string message) : base(message) { }
        public RedisConnectionClosedException(string message, Exception inner) : base(message, inner) { }
        protected RedisConnectionClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
