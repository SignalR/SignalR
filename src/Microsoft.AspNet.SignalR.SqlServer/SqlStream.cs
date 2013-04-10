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

            _sender = new SqlSender(connectionString, tableName, _trace, dbProviderFactory);
            _receiver = new SqlReceiver(connectionString, tableName, _trace, _tracePrefix, dbProviderFactory);
        }

        public event EventHandler Queried;

        public event EventHandler<SqlStreamErrrorEventArgs> Error;

        public event EventHandler<SqlStreamReceivedEventArgs> Received;

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

        private void OnError(Exception error)
        {
            if (Error != null)
            {
                Error(this, new SqlStreamErrrorEventArgs(error));
            }
        }

        private void OnQueried()
        {
            if (Queried != null)
            {
                Queried(this, EventArgs.Empty);
            }
        }

        private void OnReceived(ulong payloadId, IList<Message> messages)
        {
            if (Received != null)
            {
                Received(this, new SqlStreamReceivedEventArgs(payloadId, messages));
            }
        }
    }
}
