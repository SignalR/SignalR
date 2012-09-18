using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.SqlServer
{
    internal static class SqlCommandExtensions
    {
        public static Task<SqlDataReader> ExecuteReaderAsync(this SqlCommand command)
        {
            return Task.Factory.FromAsync(
                (cb, state) => command.BeginExecuteReader(cb, state, CommandBehavior.CloseConnection),
                iar => command.EndExecuteReader(iar),
                null);
        }

        public static Task ExecuteNonQueryAsync(this SqlCommand command)
        {
            return Task.Factory.FromAsync(
                (cb, state) => command.BeginExecuteNonQuery(cb, state),
                iar => command.EndExecuteNonQuery(iar),
                null);
        }
    }
}
