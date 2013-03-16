// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlStream
    {
        private readonly int _streamIndex;
        private readonly Action _onRetry;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;
        private readonly TraceSource _trace;

        public SqlStream(int streamIndex, string connectionString, string tableNamePrefix, Action onRetry, Func<int, ulong, IList<Message>, Task> onReceived, TraceSource trace)
        {
            _streamIndex = streamIndex;
            _onRetry = onRetry;
            _trace = trace;

            _sender = new SqlSender(connectionString, tableNamePrefix, _onRetry, _trace);
            var tableName = String.Format(CultureInfo.InvariantCulture, "{0}_{1}", tableNamePrefix, _streamIndex + 1);
            _receiver = new SqlReceiver(connectionString, tableName, _streamIndex, onReceived, _onRetry, _trace);
        }

        public Task Send(IList<Message> messages)
        {
            _trace.TraceVerbose("Saving payload of {0} messages(s) to stream {1} in SQL server", messages.Count, _streamIndex);

            return _sender.Send(messages);
        }
    }
}
