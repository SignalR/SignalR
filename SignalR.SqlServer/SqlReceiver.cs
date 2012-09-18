using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.SqlServer
{
    internal class SqlReceiver: IDisposable
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly Func<string, ulong, Message[], Task> _onReceive;
        private readonly IJsonSerializer _json;

        private string _selectSql = "SELECT PayloadId, Payload FROM {0} WHERE PayloadId > @PayloadId";
        private object _sqlDependencyInit;
        private long _lastPayloadId = 0;

        public SqlReceiver(string connectionString, string tableName, Func<string, ulong, Message[], Task> onReceive, IJsonSerializer jsonSerializer)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _onReceive = onReceive;
            _json = jsonSerializer;

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, _tableName);

            ListenForMessages();
        }

        public void Dispose()
        {
            if (_sqlDependencyInit != null)
            {
                SqlDependency.Stop(_connectionString);
            }
        }

        private void ListenForMessages()
        {
            InitSqlDependency();
            var connection = new SqlConnection(_connectionString);
            var command = BuildQueryCommand(connection);
            
            var sqlDependency = new SqlDependency(command);
            sqlDependency.OnChange += (sender, e) =>
                {
                    GetMessages()
                        .Then(hadMessages => ListenForMessages()) // TODO: Decide whether to immediately query or setup a dependency to wait
                        .Catch();
                };

            command.ExecuteReaderAsync()
                .Then(() => connection.Close())
                .Catch(_ => connection.Close());
        }

        private SqlCommand BuildQueryCommand(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = _selectSql;
            command.Parameters.AddWithValue("PayloadId", _lastPayloadId);
            return command;
        }

        private Task<bool> GetMessages()
        {
            var connection = new SqlConnection(_connectionString);
            var command = BuildQueryCommand(connection);
            return command.ExecuteReaderAsync()
                .Then(rdr =>
                    {
                        if (!rdr.HasRows)
                        {
                            connection.Close();
                            return TaskAsyncHelper.False;
                        }

                        var tcs = new TaskCompletionSource<bool>();
                        ReadRow(rdr, tcs);
                        return tcs.Task;
                    })
                .Then(hadMessages =>
                    {
                        connection.Close();
                        return hadMessages;
                    });
        }

        private void ReadRow(SqlDataReader reader, TaskCompletionSource<bool> tcs)
        {
            if (reader.Read())
            {
                _onReceive("0", (ulong)reader.GetInt64(0), _json.Parse<Message[]>(reader.GetString(1)))
                    .Then((rdr, innerTcs) => ReadRow(rdr, innerTcs), reader, tcs)
                    .Catch();
            }
            else
            {
                tcs.SetResult(true);
            }
        }

        private void InitSqlDependency()
        {
            LazyInitializer.EnsureInitialized(ref _sqlDependencyInit, () =>
                {
                    SqlDependency.Start(_connectionString);
                    var perm = new SqlClientPermission(PermissionState.Unrestricted);
                    perm.Demand();
                    return new object();
                });
        }
    }
}
