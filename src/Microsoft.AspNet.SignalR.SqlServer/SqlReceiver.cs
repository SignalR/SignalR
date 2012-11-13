// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlReceiver: IDisposable
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly Func<string, ulong, Message[], Task> _onReceive;

        private string _selectSql = "SELECT PayloadId, Payload FROM {0} WHERE PayloadId > @PayloadId";
        private string _maxIdSql = "SELECT MAX(PayloadId) FROM {0}";
        private object _sqlDependencyInit;
        private long _lastPayloadId = 0;

        public SqlReceiver(string connectionString, string tableName, Func<string, ulong, Message[], Task> onReceive)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _onReceive = onReceive;

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, _tableName);
            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, _tableName);

            GetStartingId();
            ListenForMessages();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sqlDependencyInit != null)
                {
                    SqlDependency.Stop(_connectionString);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        private void GetStartingId()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand(_maxIdSql, connection))
                {
                    var maxId = cmd.ExecuteScalar();
                    _lastPayloadId = maxId != null ? (long)maxId : _lastPayloadId;
                }
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

            connection.Open();
            command.ExecuteReaderAsync()
                .Then(() => connection.Close())
                .Catch(_ => connection.Close());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
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
            connection.Open();
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
                var id = reader.GetInt64(0);
                var messages = JsonConvert.DeserializeObject<Message[]>(reader.GetString(1));

                // Update the last payload id
                _lastPayloadId = id;

                _onReceive("0", (ulong)id, messages)
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
