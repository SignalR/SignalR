// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNet.SignalR.Redis
{
    [Serializable]
    public class RedisConnectionException : Exception
    {
        public RedisConnectionException() : base() { }
        public RedisConnectionException(string errorMessage) : base(errorMessage) { }
        public RedisConnectionException(string message, Exception inner) : base(message, inner) { }
        protected RedisConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
