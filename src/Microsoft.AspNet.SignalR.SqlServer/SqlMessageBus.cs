// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
        private const string _tableNamePrefix = "Messages";
        private readonly int _tableCount;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly TraceSource _trace;
        private readonly ReadOnlyCollection<SqlReceiver> _receivers;

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Buffering has not been implemented yet")]
        private readonly int _queueSize;

        public const int DefaultQueueSize = 1000;

        /// <summary>
        /// Creates a new instance of the SqlMessageBus class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="tableCount">The number of tables to use as "message tables".</param>
        /// <param name="queueSize">The max number of outgoing messages to queue in case SQL server goes offline.</param>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        public SqlMessageBus(string connectionString, int tableCount, int queueSize, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, queueSize, null, null, dependencyResolver)
        {

        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Review")]
        internal SqlMessageBus(string connectionString, int tableCount, int queueSize, SqlInstaller sqlInstaller, SqlSender sqlSender, IDependencyResolver dependencyResolver)
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

            if (queueSize < 1)
            {
                throw new ArgumentOutOfRangeException("queueSize", String.Format(CultureInfo.InvariantCulture, Resources.Error_ValueMustBeGreaterThan1, "queueSize"));
            }

            if (!IsSqlEditionSupported(connectionString))
            {
                throw new PlatformNotSupportedException(Resources.Error_UnsupportedSqlEdition);
            }

            _tableCount = tableCount;
            _queueSize = queueSize;
            var traceManager = dependencyResolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(SqlMessageBus).Name];

            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableNamePrefix, tableCount, Trace);
            _installer.EnsureInstalled();

            _sender = sqlSender ?? new SqlSender(connectionString, _tableNamePrefix, tableCount, Trace);
            _receivers = new ReadOnlyCollection<SqlReceiver>(
                Enumerable.Range(1, tableCount)
                    .Select(tableNumber => new SqlReceiver(connectionString,
                        String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _tableNamePrefix, tableNumber), tableNumber - 1, OnReceived, Trace))
                    .ToList()
            );

            Open();
        }

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
                for (var i = 0; i < _receivers.Count; i++)
                {
                    _receivers[i].Dispose();
                }
            }

            base.Dispose(disposing);
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

        private static class SqlEngineEdition
        {
            // See article http://msdn.microsoft.com/en-us/library/ee336261.aspx for details on EngineEdition
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification="Reviewed")]
            public static int Personal = 1;
            public static int Standard = 2;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed")]
            public static int Enterprise = 3;
            public static int Express = 4;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed")]
            public static int SqlAzure = 5;
        }
    }
}
