// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "ErrorData may not be serializable")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "ErrorData may not be serializable")]
    public class HubException : Exception
    {
        public HubException() { }
        public HubException(string message) : base(message) { }

        public HubException(string message, object errorData)
            : base(message)
        {
            ErrorData = errorData;
        }

        public object ErrorData { get; private set; }
    }
}
