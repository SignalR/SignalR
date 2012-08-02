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
    public class DisconnectHandler
    {
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _connectionCancellationTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

        public CancellationTokenSource CancelDisconnectToken(ulong connectionId)
        {
            CancellationTokenSource cts;
            _connectionCancellationTokens.TryRemove(connectionId, out cts);
            cts.Cancel();
            return cts;
        }

        public bool IsRegisteredForDisconnect(ulong connectionId)
        {
            return _connectionCancellationTokens.ContainsKey(connectionId);
        }

        public CancellationToken GetOrAddDisconnectToken(ulong connectionId)
        {
            return _connectionCancellationTokens.GetOrAdd(connectionId, new CancellationTokenSource()).Token;
        }
    }
}
