// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    internal class AckHandler : IAckHandler, IDisposable
    {
        private readonly ConcurrentDictionary<string, AckInfo> _acks = new ConcurrentDictionary<string, AckInfo>();

        // REVIEW: Consider making this pluggable
        private readonly TimeSpan _ackThreshold;

        // REVIEW: Consider moving this logic to the transport heartbeat
        private Timer _timer;

        public AckHandler()
            : this(cancelAcksOnTimeout: true, 
                   ackThreshold: TimeSpan.FromMinutes(1),
                   ackInterval: TimeSpan.FromSeconds(10))
        {
        }

        public AckHandler(bool cancelAcksOnTimeout, TimeSpan ackThreshold, TimeSpan ackInterval)
        {
            if (cancelAcksOnTimeout)
            {
                _timer = new Timer(_ => CheckAcks(), state: null, dueTime: ackInterval, period: ackInterval);
            }

            _ackThreshold = ackThreshold;
        }

        public Task CreateAck(string id)
        {
            return _acks.GetOrAdd(id, _ => new AckInfo()).Tcs.Task;
        }

        public bool TriggerAck(string id)
        {
            AckInfo info;
            if (_acks.TryRemove(id, out info))
            {
                info.Tcs.TrySetResult(null);
                return true;
            }

            return false;
        }

        private void CheckAcks()
        {
            foreach (var pair in _acks)
            {
                TimeSpan elapsed = DateTime.UtcNow - pair.Value.Created;
                if (elapsed > _ackThreshold)
                {
                    AckInfo info;
                    if (_acks.TryRemove(pair.Key, out info))
                    {
                        // If we have a pending ack for longer than the threshold
                        // cancel it.
                        info.Tcs.TrySetCanceled();
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                }

                // Trip all pending acks
                foreach (var ackInfo in _acks.Values)
                {
                    ackInfo.Tcs.TrySetCanceled();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private class AckInfo
        {
            public TaskCompletionSource<object> Tcs { get; private set; }
            public DateTime Created { get; private set; }

            public AckInfo()
            {
                Tcs = new TaskCompletionSource<object>();
                Created = DateTime.UtcNow;
            }
        }
    }
}
