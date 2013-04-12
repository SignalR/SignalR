// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlReceiver : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _streamId = "0";
        private readonly Func<string, ulong, IList<Message>, Task> _onReceive;
        private readonly TraceSource _trace;
        private readonly int[] _retryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200, 1000, 1500, 3000 };
        private readonly int _retryErrorDelay = 5000;
        private readonly bool _queryNotificationsSupported;

        private string _selectSql = "SELECT [PayloadId], [Payload] FROM [" + SqlMessageBus.SchemaName + "].[{0}] WHERE [PayloadId] > @PayloadId";
        private string _maxIdSql = "SELECT COALESCE(MAX([PayloadId]), 0) FROM [" + SqlMessageBus.SchemaName + "].[{0}]";
        private long _lastPayloadId = 0;
        private SqlCommand _receiveCommand;

        public SqlReceiver(string connectionString, string tableName, Func<string, ulong, IList<Message>, Task> onReceive, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _onReceive = onReceive;
            _trace = traceSource;

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, _tableName);
            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, _tableName);

            _queryNotificationsSupported = InitializeSqlDependency();
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
                if (_queryNotificationsSupported)
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
                    _lastPayloadId = (long)maxId;
                }
            }
        }

        private void Receive(object state)
        {
            for (var i = 0; i < _retryDelays.Length; i++)
            {
                _trace.TraceVerbose("Checking for new messages, try {0} of {1}", i + 1, _retryDelays.Length);

                // Look for new messages until we find some or retry expires
                bool foundMessages = false;
                try
                {
                    foundMessages = CheckForMessages();
                }
                catch (SqlException ex)
                {
                    // TODO: Call into base to start buffering here and kick off BG thread
                    //       to start pinging SQL server to detect when it comes back online

                    _trace.TraceError("SQL error: {0}", ex);
                    _trace.TraceVerbose("Waiting {0}ms before trying to get messages again.", _retryErrorDelay);

                    Thread.Sleep(_retryErrorDelay);

                    // Push to a new thread as this is recursive
                    ThreadPool.QueueUserWorkItem(Receive);
                    return;
                }

                if (foundMessages)
                {
                    // We found messages so start the loop again
                    _trace.TraceVerbose("Messages received, reset retry counter to 0");

                    i = -1; // loop will increment this to 0
                    continue;
                }
                else
                {
                    _trace.TraceVerbose("No messages received");
                }

                var retryDelay = _retryDelays[i];
                if (retryDelay > 0)
                {
                    _trace.TraceVerbose("Waiting {0}ms before checking for messages again", retryDelay);

                    Thread.Sleep(retryDelay);
                }

                if (i == _retryDelays.Length - 1 && !_queryNotificationsSupported)
                {
                    // Just keep looping on the last retry delay as query notifications aren't supported
                    i = _retryDelays.Length - 2;  // loop will increment this to Length - 1
                }
            }

            // No messages found so set up query notification to callback when messages are available
            _trace.TraceVerbose("Message checking max retries reached");

            SetupQueryNotification();
        }

        private bool CheckForMessages()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                UpdateQueryCommand(connection);
                connection.Open();
                return ProcessReader(_receiveCommand.ExecuteReader()) > 0;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "dummy", Justification = "Dummy value returned from lazy init routine.")]
        private void SetupQueryNotification()
        {
            Debug.Assert(_queryNotificationsSupported, "The SQL notification listener has not been initialized or is not supported.");

            _trace.TraceVerbose("Setting up SQL notification");

            using (var connection = new SqlConnection(_connectionString))
            {
                UpdateQueryCommand(connection);

                var sqlDependency = new SqlDependency(_receiveCommand);
                sqlDependency.OnChange += (sender, e) =>
                    {
                        _trace.TraceInformation("SqlDependency.OnChanged fired: {0}", e.Info);

                        _receiveCommand.Notification = null;

                        if (e.Type == SqlNotificationType.Change)
                        {
                            Receive(null);
                        }
                        else
                        {
                            // If the e.Info value here is 'Invalid', ensure the query SQL meets the requirements
                            // for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx
                            _trace.TraceError("SQL notification subscription error: {0}", e.Info);

                            // TODO: Do we need to be more paticular about the type of error here?
                            Thread.Sleep(_retryErrorDelay);
                            Receive(null);
                        }
                    };

                try
                {
                    connection.Open();
                    // Executing the query is required to set up the dependency
                    ProcessReader(_receiveCommand.ExecuteReader());

                    _trace.TraceVerbose("SQL notification set up");
                }
                catch (SqlException ex)
                {
                    // TODO: Call into base to start buffering here and kick off BG thread
                    //       to start pinging SQL server to detect when it comes back online

                    _trace.TraceError("SQL error: {0}", ex);
                    _trace.TraceVerbose("Waiting {0}ms before trying to get messages again.", _retryErrorDelay);
                    Thread.Sleep(_retryErrorDelay);

                    // Push to a new thread as this is potentially recursive
                    ThreadPool.QueueUserWorkItem(Receive);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        private void UpdateQueryCommand(SqlConnection connection)
        {
            if (_receiveCommand == null)
            {
                _receiveCommand = connection.CreateCommand();
                _receiveCommand.CommandText = _selectSql;
            }
            _receiveCommand.Connection = connection;
            _receiveCommand.Parameters.Clear();
            _receiveCommand.Parameters.AddWithValue("PayloadId", _lastPayloadId);
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
                var payload = SqlPayload.FromBytes(reader.GetSqlBinary(1).Value);

                messageCount += payload.Messages.Count;

                if (id != _lastPayloadId + 1)
                {
                    // TODO: This is not necessarily an error, identity columns can result in gaps in the sequence
                    _trace.TraceError("Missed message(s) from SQL Server. Expected payload ID {0} but got {1}.", _lastPayloadId + 1, id);
                }

                if (id <= _lastPayloadId)
                {
                    _trace.TraceInformation("Duplicate message(s) or identity column reset from SQL Server. Last payload ID {0}, this payload ID {1}", _lastPayloadId, id);
                }

                _lastPayloadId = id;

                // Pass to the underlying message bus
                _onReceive(_streamId, (ulong)id, payload.Messages);

                _trace.TraceVerbose("Payload {0} containing {1} message(s) received", id, payload.Messages.Count);
            }

            _trace.TraceVerbose("{0} payloads processed, {1} message(s) received", payloadCount, messageCount);
            return payloadCount;
        }

        private bool InitializeSqlDependency()
        {
            _trace.TraceVerbose("Starting SQL notification listener");

            bool result = false;

            try
            {
                SqlDependency.Start(_connectionString);
                result = true;

                _trace.TraceVerbose("SQL notification listener started");
            }
            catch (InvalidOperationException)
            {
                _trace.TraceInformation("SQL Service Broker is disabled, disabling query notifications");
            }
            
            return result;
        }
    }
}
