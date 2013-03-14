// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    /// <summary>
    /// Uses SQL Server tables to scale-out SignalR applications in web farms.
    /// </summary>
    public class SqlMessageBus : ScaleoutMessageBus
    {
        internal const string SchemaName = "SignalR";

        private const int DefaultBufferSize = 1000;
        private const string _tableNamePrefix = "Messages";
        
        private readonly string _connectionString;
        private readonly int _tableCount;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly TraceSource _trace;
        private readonly SqlReceiver[] _receivers;

        /// <summary>
        /// Creates a new instance of the SqlMessageBus class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="tableCount">The number of tables to use as "message tables".</param>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, dependencyResolver)
        {

        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Review")]
        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            if (tableCount < 1)
            {
                throw new ArgumentOutOfRangeException("tableCount", String.Format(CultureInfo.InvariantCulture, Resources.Error_ValueMustBeGreaterThan1, "tableCount"));
            }

            _connectionString = connectionString;

            if (!IsSqlEditionSupported(_connectionString))
            {
                throw new PlatformNotSupportedException(Resources.Error_UnsupportedSqlEdition);
            }

            _tableCount = tableCount;
            var traceManager = dependencyResolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(SqlMessageBus).Name];

            ReconnectDelay = TimeSpan.FromSeconds(2);

            _installer = sqlInstaller ?? new SqlInstaller(_connectionString, _tableNamePrefix, _tableCount, Trace);
            _installer.EnsureInstalled();

            _sender = new SqlSender(_connectionString, _tableNamePrefix, tableCount, OnError, Trace);
            _receivers = 
                Enumerable.Range(1, tableCount)
                    .Select(tableNumber => new SqlReceiver(_connectionString,
                        String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _tableNamePrefix, tableNumber), tableNumber - 1, OnReceived, OnError, Trace))
                    .ToArray();

            Open();
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override TraceSource Trace
        {
            get
            {
                return _trace;
            }
        }

        protected override int StreamCount
        {
            get
            {
                return _tableCount;
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _sender.Send(streamIndex, messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var i = 0; i < _receivers.Length; i++)
                {
                    _receivers[i].Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void OnError(Exception exception)
        {
            _trace.TraceError("SQL error: {0}", exception);

            if (exception == null || IsRecoverableException(exception))
            {
                Buffer(DefaultBufferSize);

                ThreadPool.QueueUserWorkItem(CheckForSqlAvailability);
            }
            else
            {
                CloseAndInvokeErrorCallback(exception);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is the intent")]
        private void CheckForSqlAvailability(object state)
        {
            // NOTE: Invoked from a background thread
            var available = false;
            while (true)
            {
                Thread.Sleep(ReconnectDelay);

                using (var connection = new SqlConnection(_connectionString))
                {
                    try
                    {
                        connection.Open();
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = "SELECT GETDATE()";
                        cmd.ExecuteScalar();
                        available = true;
                        break;      
                    }
                    catch (Exception ex)
                    {
                        if (!IsRecoverableException(ex))
                        {
                            // Can't recover, close the buffer and invoke the error callback
                            CloseAndInvokeErrorCallback(ex);
                            break;
                        }
                    }
                }
            }

            if (available)
            {
                // We're back up, start receivers again and stop buffering
                for (var i = 0; i < _receivers.Length; i++)
                {
                    _receivers[i].Receive();
                }
                Open();
            }
        }

        private void CloseAndInvokeErrorCallback(Exception exception)
        {
            Close(exception);
            
            // TODO: Invoke user defined error callback
        }

        private static bool IsRecoverableException(Exception exception)
        {
            var sqlException = exception as SqlException;

            return
                (exception is InvalidOperationException &&
                    String.Equals(exception.Source, "System.Data", StringComparison.OrdinalIgnoreCase) &&
                    exception.Message.StartsWith("Timeout expired", StringComparison.OrdinalIgnoreCase))
                ||
                (sqlException != null && (
                    sqlException.Number == SqlErrorNumbers.ConnectionTimeout ||
                    sqlException.Number == SqlErrorNumbers.ServerNotFound ||
                    sqlException.Number == SqlErrorNumbers.TransportLevelError ||
                    // Failed to start a MARS session due to the server being unavailable
                    (sqlException.Number == SqlErrorNumbers.Unknown && sqlException.Message.IndexOf("error: 19", StringComparison.OrdinalIgnoreCase) >= 0)
                )
            );
        }

        private static bool IsSqlEditionSupported(string connectionString)
        {
            int edition;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT SERVERPROPERTY ( 'EngineEdition' )";
                edition = (int)cmd.ExecuteScalar();
            }

            return edition >= SqlEngineEdition.Standard && edition <= SqlEngineEdition.Express;
        }

        private static class SqlErrorNumbers
        {
            // SQL error numbers: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlerror.number.aspx/html
            public const int Unknown = -1;
            public const int ConnectionTimeout = -2;
            public const int ServerNotFound = 2;
            public const int TransportLevelError = 233;
        }

        private static class SqlEngineEdition
        {
            // See article http://technet.microsoft.com/en-us/library/ms174396.aspx for details on EngineEdition
            public const int Personal = 1;
            public const int Standard = 2;
            public const int Enterprise = 3;
            public const int Express = 4;
            public const int SqlAzure = 5;
        }
    }
}
