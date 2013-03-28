// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly Action<ulong, IList<Message>> _onReceived;
        private readonly Action _onRetry;
        private readonly Action<Exception> _onError;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;

        private long? _lastPayloadId = null;
        private string _maxIdSql = "SELECT [PayloadId] FROM [{0}].[{1}_Id]";
        private string _selectSql = "SELECT [PayloadId], [Payload] FROM [{0}].[{1}] WHERE [PayloadId] > @PayloadId";
        private ObservableDbOperation _dbOperation;

        public SqlReceiver(string connectionString, string tableName, Action<ulong, IList<Message>> onReceived, Action onRetry, Action<Exception> onError, TraceSource traceSource, string tracePrefix)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _tracePrefix = tracePrefix;
            _onReceived = onReceived;
            _onRetry = onRetry;
            _onError = onError;
            _trace = traceSource;

            _maxIdSql = String.Format(CultureInfo.InvariantCulture, _maxIdSql, SqlMessageBus.SchemaName, _tableName);
            _selectSql = String.Format(CultureInfo.InvariantCulture, _selectSql, SqlMessageBus.SchemaName, _tableName);
        }

        public Task StartReceiving()
        {
            var tcs = new TaskCompletionSource<object>();

            ThreadPool.QueueUserWorkItem(Receive, tcs);

            return tcs.Task;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_dbOperation != null)
                {
                    _dbOperation.Dispose();
                }
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Class level variable"),
         SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread with explicit error processing")]
        private void Receive(object state)
        {
            var tcs = (TaskCompletionSource<object>)state;

            if (!_lastPayloadId.HasValue)
            {
                var lastPayloadIdOperation = new DbOperation(_connectionString, _maxIdSql, _trace)
                {
                    TracePrefix = _tracePrefix
                };

                try
                {
                    _lastPayloadId = (long)lastPayloadIdOperation.ExecuteScalar();
                    // Complete the StartReceiving task as we've successfully initialized the payload ID
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }
            }

            // NOTE: This is called from a BG thread so any uncaught exceptions will crash the process
            lock (this)
            {
                _dbOperation = new ObservableDbOperation(_connectionString, _selectSql, _trace, new SqlParameter("PayloadId", _lastPayloadId))
                {
                    OnRetry = o =>
                    {
                        // Recoverable error
                        o.Parameters[0].Value = _lastPayloadId;
                        _onRetry();
                    },
                    OnError = ex =>
                    {
                        // Fatal async error, e.g. from SQL notification update
                        _onError(ex);
                    },
                    TracePrefix = _tracePrefix
                };
            }

            _dbOperation.ExecuteReaderWithUpdates((rdr, o) => ProcessRecord(rdr, o));
        }

        private void ProcessRecord(IDataRecord record, DbOperation sqlOperation)
        {
            var id = record.GetInt64(0);
            var payload = SqlPayload.FromBytes(record.GetBinary(1));

            if (id != _lastPayloadId + 1)
            {
                _trace.TraceError("{0}Missed message(s) from SQL Server. Expected payload ID {1} but got {2}.", _tracePrefix, _lastPayloadId + 1, id);
            }

            if (id <= _lastPayloadId)
            {
                _trace.TraceInformation("{0}Duplicate message(s) or payload ID reset from SQL Server. Last payload ID {1}, this payload ID {2}", _tracePrefix, _lastPayloadId, id);
            }

            _lastPayloadId = id;

            // Update the Parameter with the new payload ID
            sqlOperation.Parameters[0].Value = _lastPayloadId;

            // Pass to the underlying message bus
            // TODO: What is the contract here regarding the returned task?
            _onReceived((ulong)id, payload.Messages).Wait();

            _trace.TraceVerbose("{0}Payload {1} containing {2} message(s) received", _tracePrefix, id, payload.Messages.Count);
        }
    }
}
