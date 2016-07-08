// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    [Serializable]
    public class NotAuthorizedException : Exception
    {
        public NotAuthorizedException() { }
        public NotAuthorizedException(string message) : base(message) { }
        public NotAuthorizedException(string message, Exception inner) : base(message, inner) { }
        protected NotAuthorizedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
