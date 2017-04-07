// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
