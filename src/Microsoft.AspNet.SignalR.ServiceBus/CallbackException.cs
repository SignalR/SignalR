// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Runtime.Serialization;

    class CallbackException : Exception
    {
        public CallbackException()
        {
        }

        public CallbackException(string message)
            : base(message)
        {
        }

        public CallbackException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CallbackException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
