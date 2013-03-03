// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
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
        private readonly TraceSource _trace;

        private string _selectSql = "SELECT [PayloadId], [Payload] FROM {0} WHERE [PayloadId] > @PayloadId";
        private string _maxIdSql = "SELECT MAX([PayloadId]) FROM {0}";
        private bool _sqlDependencyInitialized;
        private long _lastPayloadId = 0;
        private int _retryCount = 5;
        private int _retryDelay = 250;
        private SqlDependency _sqlDependency;

        public SqlReceiver(string connectionString, string tableName, Func<string, ulong, Message[], Task> onReceive, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _onReceive = onReceive;
            _sqlDepedencyLazyInit = new Lazy<object>(InitSqlDependency);
            _trace = traceSource;

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, _tableName);
            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, _tableName);

            InitializeLastPayloadId();
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
        private void InitializeLastPayloadId()
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
                _trace.TraceEvent(TraceEventType.Verbose, 0, "Checking for new messages, try {0} of {1}", i, _retryCount);
                // Look for new messages until we find some or retry expires
                if (CheckForMessages())
                {
                    // We found messages so start the loop again
                    _trace.TraceEvent(TraceEventType.Verbose, 0, "Messages received, reset retry counter to 0");
                    i = 0;
                }
                else
                {
                    _trace.TraceEvent(TraceEventType.Verbose, 0, "No messages received");
                }

                _trace.TraceEvent(TraceEventType.Verbose, 0, "Waiting {0}ms before checking for messages again", _retryDelay);
                Thread.Sleep(_retryDelay);
            }

            // No messages found so set up query notification to callback when messages are available
            _trace.TraceEvent(TraceEventType.Verbose, 0, "Message checking max retries reached ({0})", _retryCount);
            SetupQueryNotification();
        }

        private void SetupQueryNotification()
        {
            _trace.TraceEvent(TraceEventType.Verbose, 0, "Setting up SQL notification");
            
            var dummy = _sqlDepedencyLazyInit.Value;
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = BuildQueryCommand(connection);

                _sqlDependency = new SqlDependency(command);
                _sqlDependency.OnChange += (sender, e) =>
                    {
                        _trace.TraceInformation("SqlDependency.OnChanged fired: {0}", e.Info);

                        _sqlDependency = null;

                        if (e.Type == SqlNotificationType.Change)
                        {
                            Receive(null);
                        }
                        else
                        {
                            _trace.TraceEvent(TraceEventType.Error, 0, "SQL notification subscription error: {0}", e.Info);

                            // TODO: Do we need to more paticular about the type of error here?
                            Receive(null);
                        }
                    };

                // Executing the query is required to set up the dependency
                connection.Open();
                ProcessReader(command.ExecuteReader());
            }
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
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = BuildQueryCommand(connection);
                return ProcessReader(command.ExecuteReader()) > 0;
            }
        }

        private int ProcessReader(SqlDataReader reader)
        {
            if (!reader.HasRows)
            {
                return 0;
            }
            
            var payloadCount = 0;
            var messageCount = 0;
            while (reader.Read())
            {
                payloadCount++;
                var id = reader.GetInt64(0);
                var messages = JsonConvert.DeserializeObject<Message[]>(reader.GetString(1));
                messageCount += messages.Length;

                if (id != _lastPayloadId + 1)
                {
                    _trace.TraceEvent(TraceEventType.Error, 0, "Missed messages from SQL Server. Expected payload ID {0} but got {1}.", _lastPayloadId + 1, id);
                }

                if (id < _lastPayloadId)
                {
                    _trace.TraceEvent(TraceEventType.Information, 0, "Duplicate messages or identity column reset from SQL Server. Last payload ID {0}, this payload ID {1}", _lastPayloadId, id);
                }

                _lastPayloadId = id;

                // Queue to send to the underlying message bus
                _receiveQueue.Enqueue(() => _onReceive(_streamId, (ulong)id, messages));

                _trace.TraceEvent(TraceEventType.Verbose, 0, "Payload {0} containing {1} message(s) queued for receive to local message bus", id, messages.Length);
            }

            _trace.TraceEvent(TraceEventType.Verbose, 0, "{0} payloads processed, {1} messages received", payloadCount, messageCount);
            return payloadCount;
        }

        private object InitSqlDependency()
        {
            _trace.TraceEvent(TraceEventType.Verbose, 0, "Starting SQL notification listener");

            var perm = new SqlClientPermission(PermissionState.Unrestricted);
            perm.Demand();

            SqlDependency.Start(_connectionString);
            
            _sqlDependencyInitialized = true;
            _trace.TraceEvent(TraceEventType.Verbose, 0, "SQL notification listener started");
            return new object();
        }
    }
}
