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
        private readonly string _tableName = "SignalR_Messages";
        private readonly int _tableCount;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;
        private readonly TraceSource _trace;

        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, null, null, dependencyResolver)
        {
            
        }

        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, SqlSender sqlSender, SqlReceiver sqlReceiver, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            if (tableCount != 1)
            {
                throw new ArgumentException(Resources.Error_TableCountMustBeOne, "tableCount");
            }

            _tableCount = tableCount;
            _trace = dependencyResolver.Resolve<ITraceManager>()["SignalR." + GetType().Name];

            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableName, tableCount, _trace);
            _installer.EnsureInstalled();

            // TODO: Support tableCount
            _sender = sqlSender ?? new SqlSender(connectionString, _tableName, _trace);
            _receiver = sqlReceiver ?? new SqlReceiver(connectionString, _tableName, OnReceived, _trace);
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
