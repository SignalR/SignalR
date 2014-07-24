// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
        private readonly TraceSource _trace;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;
        private readonly string _tracePrefix;

        public SqlStream(int streamIndex, string connectionString, string tableName, TraceSource traceSource, IDbProviderFactory dbProviderFactory)
        {
            _streamIndex = streamIndex;
            _trace = traceSource;
            _tracePrefix = String.Format(CultureInfo.InvariantCulture, "Stream {0} : ", _streamIndex);

            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };

            _sender = new SqlSender(connectionString, tableName, _trace, dbProviderFactory);
            _receiver = new SqlReceiver(connectionString, tableName, _trace, _tracePrefix, dbProviderFactory);
            _receiver.Queried += () => Queried();
            _receiver.Faulted += (ex) => Faulted(ex);
            _receiver.Received += (id, messages) => Received(id, messages);
        }

        public event Action Queried;

        public event Action<ulong, ScaleoutMessage> Received;

        public event Action<Exception> Faulted;

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
            _trace.TraceInformation("{0}Disposing stream {1}", _tracePrefix, _streamIndex);

            _receiver.Dispose();
        }
    }
}
