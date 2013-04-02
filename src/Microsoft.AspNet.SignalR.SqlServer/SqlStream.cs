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
        private readonly Action _open;
        private readonly Action<Exception> _onError;
        private readonly TraceSource _trace;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;
        private readonly string _tracePrefix;

        private volatile bool _failed;

        public SqlStream(int streamIndex, string connectionString, string tableName, Action open, Action<int, ulong, IList<Message>> onReceived, Action<Exception> onError, TraceSource traceSource, IDbProviderFactory dbProviderFactory)
        {
            _streamIndex = streamIndex;
            _open = open;
            _onError = onError;
            _trace = traceSource;
            _tracePrefix = String.Format(CultureInfo.InvariantCulture, "Stream {0} : ", _streamIndex);

            _sender = new SqlSender(connectionString, tableName, _trace, dbProviderFactory);
            _receiver = new SqlReceiver(connectionString, tableName,
                onQuery: () => ClearFailed(),
                onReceived: (id, messages) => onReceived(_streamIndex, id, messages),
                onError: _onError,
                traceSource: _trace,
                tracePrefix: _tracePrefix,
                dbProviderFactory: dbProviderFactory);
        }

        public Task StartReceiving()
        {
            return _receiver.StartReceiving();
        }

        public Task Send(IList<Message> messages)
        {
            _trace.TraceVerbose("{0}Saving payload with {1} messages(s) to SQL server", _tracePrefix, messages.Count, _streamIndex);

            try
            {
                return _sender.Send(messages)
                    .Then(() =>
                    {
                        if (ClearFailed())
                        {
                            _receiver.StartReceiving();
                        };
                    })
                    .Catch(_ => SetFailed());
            }
            catch (Exception)
            {
                SetFailed();
                throw;
            }
        }

        public void Dispose()
        {
            _receiver.Dispose();
        }

        private void SetFailed()
        {
            lock (this)
            {
                _trace.TraceWarning("{0}Stream set to failed", _tracePrefix);
                
                _failed = true;
            }
        }

        private bool ClearFailed()
        {
            lock (this)
            {
                if (_failed)
                {
                    _failed = false;
                    _open();

                    _trace.TraceWarning("{0}Stream re-opened", _tracePrefix);

                    return true;
                }
                return false;
            }
        }
    }
}
