// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification="Should never have inner exceptions")]
    [Serializable]
    public class SqlMessageBusException : Exception
    {
        public SqlMessageBusException(string message)
            : base(message)
        {

        }
    }
}
