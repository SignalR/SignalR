// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
