// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public class SqlMessageBus : ScaleoutMessageBus
    {
        internal const string SchemaName = "SignalR";
        private const string _tableNamePrefix = "Messages";
        private readonly int _tableCount;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly TraceSource _trace;
        private readonly ReadOnlyCollection<SqlReceiver> _receivers;

        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, null, dependencyResolver)
        {

        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Review")]
        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, SqlSender sqlSender, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            if (tableCount < 1)
            {
                throw new ArgumentOutOfRangeException("tableCount", Resources.Error_TableCountOutOfRange);
            }

            _tableCount = tableCount;
            var traceManager = dependencyResolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(SqlMessageBus).Name];

            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableNamePrefix, tableCount, Trace);
            _installer.EnsureInstalled();

            _sender = sqlSender ?? new SqlSender(connectionString, _tableNamePrefix, tableCount, Trace);
            _receivers = new ReadOnlyCollection<SqlReceiver>(
                Enumerable.Range(1, tableCount)
                    .Select(tableNumber => new SqlReceiver(connectionString,
                        String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _tableNamePrefix, tableNumber), OnReceived, Trace))
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
    }
}
