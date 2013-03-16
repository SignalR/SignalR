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
        private readonly int _streamId;
        private readonly string _tracePrefix;
        private readonly Func<int, ulong, IList<Message>, Task> _onReceive;
        private readonly Action _onRetry;
        private readonly TraceSource _trace;
        private readonly int[] _retryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200, 1000, 1500, 3000 };
        
        private string _selectSql = "SELECT [PayloadId], [Payload] FROM [{0}].[{1}] WHERE [PayloadId] > @PayloadId";
        private string _maxIdSql = "SELECT [PayloadId] FROM [{0}].[{1}_Id]";
        private long _lastPayloadId = 0;
        private SqlCommand _receiveCommand;
        private bool _useQueryNotifications;

        public SqlReceiver(string connectionString, string tableName, int streamId, Func<int, ulong, IList<Message>, Task> onReceive, Action onRetry, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _streamId = streamId;
            _tracePrefix = "Stream " + _streamId + " : ";
            _onReceive = onReceive;
            _onRetry = onRetry;
            _trace = traceSource;

            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, SqlMessageBus.SchemaName, _tableName);
            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, SqlMessageBus.SchemaName, _tableName);

            _useQueryNotifications = InitializeSqlDependency();
            InitializeLastPayloadId();

            Receive();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Starts the receive loop on a new background thread
        /// </summary>
        public void Receive()
        {
            ThreadPool.QueueUserWorkItem(Receive);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Disposing")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    SqlDependency.Stop(_connectionString);
                }
                catch (Exception) { }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
        private void InitializeLastPayloadId()
        {
            var operation = new SqlOperation(_connectionString, _maxIdSql);
            _lastPayloadId = (int)operation.ExecuteScalar();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread with explicit error processing")]
        private void Receive(object state)
        {
            // NOTE: This is called from a BG thread so any uncaught exceptions will crash the process

            for (var i = 0; i < _retryDelays.Length; i++)
            {
                TraceVerbose("Checking for new messages, try {0} of {1}", i + 1, _retryDelays.Length);

                // Look for new messages until we find some or retry expires
                bool foundMessages = false;
                try
                {
                    foundMessages = CheckForMessages();
                }
                catch (Exception ex)
                {
                    // Invoke the error handler and exit, if it recovers then Receive will be called again
                    _onError(ex);
                    return;
                }

                if (foundMessages)
                {
                    // We found messages so start the loop again
                    TraceVerbose("Messages received, reset retry counter to 0");

                    i = -1; // loop will increment this to 0
                    continue;
                }
                else
                {
                    TraceVerbose("No messages received");
                }

                var retryDelay = _retryDelays[i];
                if (retryDelay > 0)
                {
                    TraceVerbose("Waiting {0}ms before checking for messages again", retryDelay);

                    Thread.Sleep(retryDelay);
                }

                if (i == _retryDelays.Length - 1 && !_useQueryNotifications)
                {
                    // Just keep looping on the last retry delay as query notifications aren't supported
                    i = _retryDelays.Length - 2;  // loop will increment this to Length - 1
                }
            }

            // No messages found so set up query notification to callback when messages are available
            TraceVerbose("Message checking max retries reached");

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread with explicit error processing"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "dummy", Justification = "Dummy value returned from lazy init routine.")]
        private void SetupQueryNotification()
        {
            Debug.Assert(_useQueryNotifications, "The SQL notification listener has not been initialized or is not supported.");

            TraceVerbose("Setting up SQL notification");

            using (var connection = new SqlConnection(_connectionString))
            {
                UpdateQueryCommand(connection);

                _receiveCommand.Notification = null;

                var sqlDependency = new SqlDependency(_receiveCommand);
                sqlDependency.OnChange += SqlDependency_OnChange;

                try
                {
                    connection.Open();
                    
                    // Executing the query is required to set up the dependency
                    ProcessReader(_receiveCommand.ExecuteReader());

                    TraceInformation("SQL notification set up");
                }
                catch (Exception ex)
                {
                    // Invoke the error handler and exit, if it recovers then Receive will be called again
                    _onError(ex);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread with explicit error processing")]
        private void SqlDependency_OnChange(object sender, SqlNotificationEventArgs args)
        {
            // NOTE: This is called from a ThreadPool thread

            TraceInformation("SQL query notification received: Type={0}, Source={1}, Info={2}", args.Type, args.Source, args.Info);

            _receiveCommand.Notification = null;

            if (args.Type == SqlNotificationType.Change)
            {
                if (args.Info == SqlNotificationInfo.Insert
                    || args.Info == SqlNotificationInfo.Expired
                    || args.Info == SqlNotificationInfo.Resource)
                {
                    Receive(null);
                }
                else if (args.Info == SqlNotificationInfo.Restart)
                {
                    TraceWarning("SQL Server restarting, starting buffering");

                    _onError(null);
                }
                else if (args.Info == SqlNotificationInfo.Error)
                {
                    TraceWarning("SQL notification error likely due to server becoming unavailable, starting buffering");

                    _onError(null);
                }
                else
                {
                    TraceError("Unexpected SQL notification details: Type={0}, Source={1}, Info={2}", args.Type, args.Source, args.Info);

                    _onError(new SqlMessageBusException(String.Format(CultureInfo.InvariantCulture, Resources.Error_UnexpectedSqlNotificationType, args.Type, args.Source, args.Info)));
                }
            }
            else if (args.Type == SqlNotificationType.Subscribe)
            {
                Debug.Assert(args.Info != SqlNotificationInfo.Invalid, "Ensure the query SQL meets the requirements for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx");
                
                TraceError("SQL notification subscription error: Type={0}, Source={1}, Info={2}", args.Type, args.Source, args.Info);

                if (args.Info == SqlNotificationInfo.TemplateLimit)
                {
                    // We've hit a subscription limit, let's back off for a bit
                    _onError(_streamId, null);
                }
                else
                {
                    // Unknown subscription error, let's stop using query notifications
                    _useQueryNotifications = false;
                    try
                    {
                        SqlDependency.Stop(_connectionString);
                    }
                    catch (Exception) { }

                    Receive(null);
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
                    TraceError("Missed message(s) from SQL Server. Expected payload ID {0} but got {1}.", _lastPayloadId + 1, id);
                }

                if (id <= _lastPayloadId)
                {
                    TraceInformation("Duplicate message(s) or identity column reset from SQL Server. Last payload ID {0}, this payload ID {1}", _lastPayloadId, id);
                }

                _lastPayloadId = id;

                // Pass to the underlying message bus
                _onReceive(_streamId, (ulong)id, payload.Messages);

                TraceVerbose("Payload {0} containing {1} message(s) received", id, payload.Messages.Count);
            }

            TraceVerbose("{0} payloads processed, {1} message(s) received", payloadCount, messageCount);
            return payloadCount;
        }

        private bool InitializeSqlDependency()
        {
            TraceVerbose("Starting SQL notification listener");

            bool result = false;

            try
            {
                SqlDependency.Start(_connectionString);
                result = true;

                TraceVerbose("SQL notification listener started");
            }
            catch (InvalidOperationException)
            {
                TraceInformation("SQL Service Broker is disabled, disabling query notifications");
            }
            
            return result;
        }

        private void TraceError(string msg, params object[] args)
        {
            _trace.TraceError(_tracePrefix + msg, args);
        }

        private void TraceWarning(string msg)
        {
            _trace.TraceWarning(_tracePrefix + msg);
        }

        private void TraceInformation(string msg)
        {
            _trace.TraceInformation(_tracePrefix + msg);
        }

        private void TraceInformation(string msg, params object[] args)
        {
            _trace.TraceInformation(_tracePrefix + msg, args);
        }

        private void TraceVerbose(string msg)
        {
            _trace.TraceVerbose(_tracePrefix + msg);
        }

        private void TraceVerbose(string msg, params object[] args)
        {
            _trace.TraceVerbose(_tracePrefix + msg, args);
        }
    }
}
