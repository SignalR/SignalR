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
        private readonly SafeSet<ITrackingDisconnect> _connections = new SafeSet<ITrackingDisconnect>(new ConnectionIdEqualityComparer());
        private readonly ConcurrentDictionary<ITrackingDisconnect, DateTime> _connectionMetadata = new ConcurrentDictionary<ITrackingDisconnect, DateTime>(new ConnectionIdEqualityComparer());
        private readonly Timer _timer;
        private TimeSpan _heartBeatInterval;
        private bool _running;

        private TransportHeartBeat()
        {
            _heartBeatInterval = TimeSpan.FromSeconds(10);
            DisconnectTimeout = TimeSpan.FromSeconds(20);

            // REVIEW: When to dispose the timer?
            _timer = new Timer(Beat,
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

        private void Beat(object state)
        {
            if (_running)
            {
                Trace.TraceInformation("SIGNALR: TransportHeatBeat timer handler took longer than current interval");
                return;
            }

            try
            {
                _running = true;

                foreach (var connection in _connections.GetSnapshot())
                {
                    if (!connection.IsAlive)
                    {
                        // The transport is currently disconnected, it could just be reconnecting though
                        // so we need to check it's last active time to see if it's over the disconnect
                        // threshold
                        TimeSpan elapsed;
                        if (TryGetElapsed(connection, out elapsed))
                        {
                            // The threshold for disconnect is the transport threshold + (potential network issues)
                            var threshold = connection.DisconnectThreshold + DisconnectTimeout;

                            if (elapsed < threshold)
                            {
                                continue;
                            }
                        }

                        try
                        {
                            // Remove the connection from the list
                            RemoveConnection(connection);

                            // Fire disconnect on the connection
                            connection.Disconnect();
                        }
                        catch
                        {
                            // Swallow exceptions that might happen during disconnect
                        }
                    }
                    else
                    {
                        // The connection is still alive so we need to keep it alive with a server side "ping".
                        // This is for scenarios where networing hardware (proxies, loadbalancers) get in the way
                        // of us handling timeout's or disconencts gracefully
                        MarkConnection(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("SignalR error during transport heart beat on background thread: {0}", ex);
            }
            finally
            {
                _running = false;
            }
        }

        private bool TryGetElapsed(ITrackingDisconnect connection, out TimeSpan elapsed)
        {
            DateTime lastUsed;
            if (_connectionMetadata.TryGetValue(connection, out lastUsed))
            {
                // Calculate how long this connection has been inactive
                elapsed = DateTime.UtcNow - lastUsed;
                return true;
            }

            elapsed = TimeSpan.Zero;
            return false;
        }

        private class ConnectionIdEqualityComparer : IEqualityComparer<ITrackingDisconnect>
        {
            public bool Equals(ITrackingDisconnect x, ITrackingDisconnect y)
            {
                return String.Equals(x.ConnectionId, y.ConnectionId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ITrackingDisconnect obj)
            {
                return obj.ConnectionId.GetHashCode();
            }
        }
    }
}