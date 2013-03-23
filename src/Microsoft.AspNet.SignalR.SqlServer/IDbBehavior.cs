// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public interface IDbBehavior
    {
        bool StartSqlDependencyListener();
        IList<Tuple<int, int>> UpdateLoopRetryDelays { get; }
        void AddSqlDependency(IDbCommand command, Action<SqlNotificationEventArgs> callback);
    }
}
