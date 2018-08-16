// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
#if NET40 || NET45 || NETSTANDARD2_0
    [Serializable]
#elif NETSTANDARD1_3
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif
    public class SlowCallbackException : Exception
    {
        public SlowCallbackException() { }
        public SlowCallbackException(string message) : base(message) { }
        public SlowCallbackException(string message, Exception inner) : base(message, inner) { }

#if NET40 || NET45 || NETSTANDARD2_0
        protected SlowCallbackException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
#elif NETSTANDARD1_3
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif
    }
}
