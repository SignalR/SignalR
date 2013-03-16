// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal static class SqlCommandExtensions
    {
        public static Task<int> ExecuteNonQueryAsync(this SqlCommand command)
        {
            return Task.Factory.FromAsync(
                (cb, state) => command.BeginExecuteNonQuery(cb, state),
                iar => command.EndExecuteNonQuery(iar),
                null);
        }
    }
}
