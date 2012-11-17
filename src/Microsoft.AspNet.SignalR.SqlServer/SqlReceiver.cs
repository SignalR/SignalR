// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlReceiver: IDisposable
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _streamId = "0";
        private readonly Func<string, ulong, Message[], Task> _onReceive;
        private readonly TaskQueue _receiveQueue = new TaskQueue();
        private readonly Lazy<object> _sqlDepedencyLazyInit;

        private string _selectSql = "SELECT PayloadId, Payload FROM {0} WHERE PayloadId > @PayloadId";
        private string _maxIdSql = "SELECT MAX(PayloadId) FROM {0}";
        private bool _sqlDependencyInitialized;
        private long _lastPayloadId = 0;
        private int _retryCount = 5;
        private int _retryDelay = 250;

        public SqlReceiver(string connectionString, string tableName, Func<string, ulong, Message[], Task> onReceive)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _onReceive = onReceive;
            _sqlDepedencyLazyInit = new Lazy<object>(InitSqlDependency);

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, _tableName);
            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, _tableName);

            GetStartingId();
            ThreadPool.QueueUserWorkItem(Receive);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sqlDependencyInitialized)
                {
                    SqlDependency.Stop(_connectionString);
                }
            }
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
                    _lastPayloadId = maxId != null && maxId != DBNull.Value ? (long)maxId : _lastPayloadId;
                }
            }
        }

        private void Receive(object state)
        {
            for (var i = 0; i <= _retryCount; i++)
            {
                // Look for new messages until we find some or retry expires
                if (CheckForMessages())
                {
                    // We found messages so start the loop again
                    i = 0;
                }
                Thread.Sleep(_retryDelay);
            }

            // No messages found so set up query notification to callback when messages are available
            SetupQueryNotification();
        }

        private void SetupQueryNotification()
        {
            var dummy = _sqlDepedencyLazyInit.Value;
            var connection = new SqlConnection(_connectionString);
            var command = BuildQueryCommand(connection);

            var sqlDependency = new SqlDependency(command);
            sqlDependency.OnChange += (sender, e) =>
                {
                    if (e.Type == SqlNotificationType.Change)
                    {
                        Receive(null);
                    }
                    else
                    {
                        // Probably a timeout or an error, just set it up again
                        SetupQueryNotification();
                    }
                };

            // Executing the query is required to set up the dependency
            connection.Open();
            command.ExecuteReader();
            connection.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        private SqlCommand BuildQueryCommand(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = _selectSql;
            command.Parameters.AddWithValue("PayloadId", _lastPayloadId);
            return command;
        }

        private bool CheckForMessages()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            var command = BuildQueryCommand(connection);
            var reader = command.ExecuteReader();
            
            if (!reader.HasRows)
            {
                connection.Close();
                return false;
            }

            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var messages = JsonConvert.DeserializeObject<Message[]>(reader.GetString(1));

                _lastPayloadId = id;

                // Queue to send to the underlying message bus
                _receiveQueue.Enqueue(() => _onReceive(_streamId, (ulong)id, messages));
            }

            connection.Close();
            return true;
        }

        private object InitSqlDependency()
        {
            var perm = new SqlClientPermission(PermissionState.Unrestricted);
            perm.Demand();

            SqlDependency.Start(_connectionString);
            
            _sqlDependencyInitialized = true;
            return new object();
        }
    }
}
