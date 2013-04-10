// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlStreamErrrorEventArgs : EventArgs
    {
        public SqlStreamErrrorEventArgs(Exception error)
        {
            Error = error;
        }

        public Exception Error { get; private set; }
    }
}
