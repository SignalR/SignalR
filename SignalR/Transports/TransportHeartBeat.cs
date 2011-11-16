using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class TransportHeartBeat : ITransportHeartBeat
    {
        private readonly static TransportHeartBeat _instance = new TransportHeartBeat();
        private readonly SafeSet<ITrackingDisconnect> _connections = new SafeSet<ITrackingDisconnect>(new ClientIdEqualityComparer());
        private readonly ConcurrentDictionary<ITrackingDisconnect, DateTime> _connectionMetadata = new ConcurrentDictionary<ITrackingDisconnect, DateTime>(new ClientIdEqualityComparer());
        private readonly Timer _timer;
        private TimeSpan _heartBeatInterval;
        private bool _running;

        private TransportHeartBeat()
        {
            _heartBeatInterval = TimeSpan.FromSeconds(10);
            DisconnectTimeout = TimeSpan.FromSeconds(20);

            // REVIEW: When to dispose the timer?
            _timer = new Timer(_ => Beat(),
                               null,
                               _heartBeatInterval,
                               _heartBeatInterval);
        }

        public static TransportHeartBeat Instance
        {
            get { return _instance; }
        }

        public TimeSpan HeartBeatInterval
        {
            get { return _heartBeatInterval; }
            set
            {
                _heartBeatInterval = value;
                if (_timer != null)
                {
                    _timer.Change(_heartBeatInterval, _heartBeatInterval);
                }
            }
        }

        public TimeSpan DisconnectTimeout
        {
            get;
            set;
        }

        public void AddConnection(ITrackingDisconnect connection)
        {
            // Remove and re-add the connection so we have the correct object reference
            _connections.Remove(connection);
            _connections.Add(connection);

            // Remove the metadata for new connections
            DateTime removed;
            _connectionMetadata.TryRemove(connection, out removed);
        }

        private void RemoveConnection(ITrackingDisconnect connection)
        {
            // Remove the connection and associated metadata
            _connections.Remove(connection);
            DateTime removed;
            _connectionMetadata.TryRemove(connection, out removed);
        }

        public void MarkConnection(ITrackingDisconnect connection)
        {
            // Mark this time this connection was used
            _connectionMetadata[connection] = DateTime.UtcNow;
        }

        private void Beat()
        {
            if (_running)
            {
                Debug.WriteLine("SIGNALR: TransportHeatBeat timer handler took longer than current interval");
                return;
            }

            _running = true;

            try
            {
                Parallel.ForEach(_connections.GetSnapshot(), (connection) =>
                {
                    if (!connection.IsAlive)
                    {
                        // The transport is currently disconnected, it could just be reconnecting though
                        // so we need to check it's last active time to see if it's over the disconnect
                        // threshold
                        DateTime lastUsed;
                        if (_connectionMetadata.TryGetValue(connection, out lastUsed))
                        {
                            // Calculate how long this connection has been inactive
                            var elapsed = DateTime.UtcNow - lastUsed;

                            // The threshold for disconnect is the long poll delay + (potential network issues)
                            // TODO: Refactor to not take a dependency on LongPollingTransport here, might want to
                            //       re-use this for other transports, e.g. ForeverTransport
                            var threshold = TimeSpan.FromMilliseconds(LongPollingTransport.LongPollDelay) +
                                            DisconnectTimeout;

                            if (elapsed < threshold)
                            {
                                return;
                            }
                        }

                        try
                        {
                            connection.Disconnect();
                        }
                        finally
                        {
                            // Remove the connection from the list
                            RemoveConnection(connection);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("SignalR error during transport heart beat: {0}", ex);
            }

            _running = false;
        }

        private class ClientIdEqualityComparer : IEqualityComparer<ITrackingDisconnect>
        {
            public bool Equals(ITrackingDisconnect x, ITrackingDisconnect y)
            {
                return String.Equals(x.ClientId, y.ClientId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ITrackingDisconnect obj)
            {
                return obj.ClientId.GetHashCode();
            }
        }
    }
}