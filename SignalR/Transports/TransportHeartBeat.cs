﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class TransportHeartBeat : ITransportHeartBeat
    {
        private readonly SafeSet<ITrackingConnection> _connections = new SafeSet<ITrackingConnection>(new ConnectionIdEqualityComparer());
        private readonly ConcurrentDictionary<ITrackingConnection, ConnectionMetadata> _connectionMetadata = new ConcurrentDictionary<ITrackingConnection, ConnectionMetadata>(new ConnectionIdEqualityComparer());
        private readonly Timer _timer;
        private readonly IConfigurationManager _configurationManager;

        private int _running;

        public TransportHeartBeat(IDependencyResolver resolver)
        {
            _configurationManager = resolver.Resolve<IConfigurationManager>();

            // REVIEW: When to dispose the timer?
            _timer = new Timer(Beat,
                               null,
                               _configurationManager.HeartBeatInterval,
                               _configurationManager.HeartBeatInterval);
        }

        public void AddConnection(ITrackingConnection connection)
        {
            // Remove and re-add the connection so we have the correct object reference
            _connections.Remove(connection);
            _connections.Add(connection);

            // Remove the metadata for new connections
            ConnectionMetadata old;
            _connectionMetadata.TryRemove(connection, out old);
            _connectionMetadata.TryAdd(connection, new ConnectionMetadata());
        }

        private void RemoveConnection(ITrackingConnection connection)
        {
            // Remove the connection and associated metadata
            _connections.Remove(connection);
            ConnectionMetadata old;
            _connectionMetadata.TryRemove(connection, out old);
        }

        public void MarkConnection(ITrackingConnection connection)
        {
            // Mark this time this connection was used
            var metadata = _connectionMetadata.GetOrAdd(connection, _ => new ConnectionMetadata());
            metadata.LastMarked = DateTime.UtcNow;
        }

        private void Beat(object state)
        {
            if (Interlocked.Exchange(ref _running, 1) == 1)
            {
                Trace.TraceInformation("SIGNALR: TransportHeatBeat timer handler took longer than current interval");
                return;
            }

            try
            {
                foreach (var connection in _connections.GetSnapshot())
                {
                    if (!connection.IsAlive)
                    {
                        // The transport is currently disconnected, it could just be reconnecting though
                        // so we need to check it's last active time to see if it's over the disconnect
                        // threshold
                        TimeSpan elapsed;
                        if (TryGetElapsed(connection, metadata => metadata.LastMarked, out elapsed))
                        {
                            // The threshold for disconnect is the transport threshold + (potential network issues)
                            var threshold = connection.DisconnectThreshold + _configurationManager.DisconnectTimeout;

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
                        TimeSpan elapsed;
                        if (!connection.IsTimedOut && 
                            TryGetElapsed(connection, metadata => metadata.Initial, out elapsed) &&
                            elapsed >= _configurationManager.ReconnectionTimeout)
                        {
                            // If we're past the expiration time then just timeout the connection                            
                            connection.Timeout();
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("SignalR error during transport heart beat on background thread: {0}", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private bool TryGetElapsed(ITrackingConnection connection, Func<ConnectionMetadata, DateTime> selector, out TimeSpan elapsed)
        {
            ConnectionMetadata metadata;
            if (_connectionMetadata.TryGetValue(connection, out metadata))
            {
                // Calculate how long this connection has been inactive
                elapsed = DateTime.UtcNow - selector(metadata);
                return true;
            }

            elapsed = TimeSpan.Zero;
            return false;
        }

        private class ConnectionIdEqualityComparer : IEqualityComparer<ITrackingConnection>
        {
            public bool Equals(ITrackingConnection x, ITrackingConnection y)
            {
                return String.Equals(x.ConnectionId, y.ConnectionId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ITrackingConnection obj)
            {
                return obj.ConnectionId.GetHashCode();
            }
        }

        private class ConnectionMetadata
        {
            public ConnectionMetadata()
            {
                Initial = DateTime.UtcNow;
                LastMarked = DateTime.UtcNow;
            }

            public DateTime LastMarked { get; set; }
            public DateTime Initial { get; set; }
        }
    }
}