using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;
using System.Reflection;
using System.Diagnostics;

namespace SignalR
{
    public class CancellationTokenResolver
    {
        private ConcurrentDictionary<ulong, CancellationTokenSource> _connectionCancellationTokens;

        public CancellationTokenResolver()
        {
            _connectionCancellationTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
        }

        public CancellationTokenSource CancelToken(ulong connectionId)
        {
            CancellationTokenSource cts;
            _connectionCancellationTokens.TryRemove(connectionId, out cts);
            cts.Cancel();
            return cts;
        }

        public bool IsRegisteredForCancellation(ulong connectionId)
        {
            return _connectionCancellationTokens.ContainsKey(connectionId);
        }

        public CancellationToken GetOrAddCancellationToken(ulong connectionId)
        {
            CancellationTokenSource ct;

            if (!_connectionCancellationTokens.TryGetValue(connectionId, out ct))
            {
                ct = new CancellationTokenSource();
            }

            return _connectionCancellationTokens.GetOrAdd(connectionId, ct).Token;
        }
    }
}
