// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal static class IDbCommandExtensions
    {
        public static void AddSqlDependency(this IDbCommand command, Action<SqlNotificationEventArgs> callback)
        {
            var sqlCommand = command as SqlCommand;
            if (sqlCommand == null)
            {
                throw new InvalidOperationException("");
            }

            var dependency = new SqlDependency(sqlCommand);
            dependency.OnChange += (o, e) => callback(e);
        }

        public static Task<int> ExecuteNonQueryAsync(this IDbCommand command)
        {
            var sqlCommand = command as SqlCommand;

            if (sqlCommand != null)
            {
                return Task.Factory.FromAsync(
                    (cb, state) => sqlCommand.BeginExecuteNonQuery(cb, state),
                    iar => sqlCommand.EndExecuteNonQuery(iar),
                    null);
            }
            else
            {
                return TaskAsyncHelper.FromResult(command.ExecuteNonQuery());
            }
        }
    }
}
