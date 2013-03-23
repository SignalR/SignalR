﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlStream : IDisposable
    {
        private readonly int _streamIndex;
        private readonly Action _onRetry;
        private readonly Action<Exception> _onError;
        private readonly SqlSender _sender;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Keep it rooted with the SqlStream")]
        private readonly SqlReceiver _receiver;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;

        public SqlStream(int streamIndex, string connectionString, string tableName, Func<int, ulong, IList<Message>, Task> onReceived, Action onRetry, Action<Exception> onError, TraceSource traceSource)
        {
            _streamIndex = streamIndex;
            _onRetry = onRetry;
            _onError = onError;
            _trace = traceSource;
            _tracePrefix = String.Format(CultureInfo.InvariantCulture, "Stream {0} : ", _streamIndex);

            _sender = new SqlSender(connectionString, tableName, _onRetry, _trace);
            _receiver = new SqlReceiver(connectionString, tableName, (id, messages) => onReceived(_streamIndex, id, messages), _onRetry, _onError, _trace, _tracePrefix);
        }

        public Task StartReceiving()
        {
            return _receiver.StartReceiving();
        }

        public Task Send(IList<Message> messages)
        {
            _trace.TraceVerbose("{0}Saving payload with {1} messages(s) to SQL server", _tracePrefix, messages.Count, _streamIndex);

            return _sender.Send(messages);
        }

        public void Dispose()
        {
            _receiver.Dispose();
        }
    }
}
