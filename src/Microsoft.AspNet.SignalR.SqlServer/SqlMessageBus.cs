// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public class SqlMessageBus : ScaleoutMessageBus
    {
        internal const string SchemaName = "SignalR";
        private const string _tableName = "Messages";
        private readonly int _tableCount;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;
        private readonly TraceSource _trace;

        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, null, null, dependencyResolver)
        {
            
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification="Review")]
        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, SqlSender sqlSender, SqlReceiver sqlReceiver, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            if (tableCount != 1)
            {
                // TODO: Support tableCount
                throw new ArgumentException(Resources.Error_TableCountMustBeOne, "tableCount");
            }

            _tableCount = tableCount;
            var traceManager = dependencyResolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(SqlMessageBus).Name];

            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableName, tableCount, Trace);
            _installer.EnsureInstalled();

            _sender = sqlSender ?? new SqlSender(connectionString, _tableName, Trace);
            _receiver = sqlReceiver ?? new SqlReceiver(connectionString, _tableName, OnReceived, Trace);
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
            // TODO: Support streamIndex/tableCount
            return _sender.Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _receiver.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
