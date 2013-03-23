// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public interface IDbBehavior
    {
        bool StartSqlDependencyListener();
        int[][] UpdateLoopRetryDelays { get; }
        void AddSqlDependency(IDbCommand command, Action<SqlNotificationEventArgs> callback);
        bool IsRecoverableException(Exception exception);
    }
}
