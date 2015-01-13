// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
#if NET4 || NET45
    [Serializable]
#endif
    public class SlowCallbackException : Exception
    {
        public SlowCallbackException() { }
        public SlowCallbackException(string message) : base(message) { }
        public SlowCallbackException(string message, Exception inner) : base(message, inner) { }

#if NET4 || NET45
        protected SlowCallbackException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
#endif
    }
}
