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
            throw new NotImplementedException();
            //return command.ExecuteReaderAsync()
            //    .Then(t =>
            //    {
            //        var rdr = t.Result;
            //        object result = null;
            //        if (rdr.Read())
            //        {
            //            result = rdr.IsDBNull(0) ? null : rdr[0];
            //        }
            //        return result;
            //    });
        }

        internal static Task<TResult> ExecuteScalarAsync<TResult>(this SqlCommand command)
        {
            throw new NotImplementedException();
            //Type type = Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult);
            //return command.ExecuteScalarAsync()
            //    .Then(t => (TResult)Convert.ChangeType(t.Result, type));
        }
    }
}