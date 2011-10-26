using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SignalR.ScaleOut
{
    internal static class SqlAsyncExtensions
    {
        internal static Task<SqlDataReader> ExecuteReaderAsync(this SqlCommand command)
        {
            return Task.Factory.FromAsync<SqlDataReader>(command.BeginExecuteReader, command.EndExecuteReader, null);
        }

        internal static Task<int> ExecuteNonQueryAsync(this SqlCommand command)
        {
            return Task.Factory.FromAsync<int>(command.BeginExecuteNonQuery, command.EndExecuteNonQuery, null);
        }

        internal static Task<object> ExecuteScalarAsync(this SqlCommand command)
        {
            return command.ExecuteReaderAsync()
                .Success(t =>
                {
                    var rdr = t.Result;
                    object result = null;
                    if (rdr.Read())
                    {
                        result = rdr.IsDBNull(0) ? null : rdr[0];
                    }
                    return result;
                });
        }

        internal static Task<TResult> ExecuteScalarAsync<TResult>(this SqlCommand command)
        {
            Type type = Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult);
            return command.ExecuteScalarAsync()
                .Success(t => (TResult)Convert.ChangeType(t.Result, type));
        }
    }
}