using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;
using System.Diagnostics;

namespace SignalR.Transports {
    internal class TransportHeartBeat {
        private readonly static TransportHeartBeat _instance = new TransportHeartBeat();
        private readonly SafeSet<ITrackingDisconnect> _connections = new SafeSet<ITrackingDisconnect>(new ClientIdEqualityComparer());
        private readonly Timer _timer;


        private TransportHeartBeat() {
            HeartBeatInterval = TimeSpan.FromSeconds(30);
            // REVIEW: When to dispose the timer?
            _timer = new Timer(_ => Beat(),
                               null,
                               HeartBeatInterval,
                               HeartBeatInterval);
        }

        public static TransportHeartBeat Instance {
            get { return _instance; }
        }

        public TimeSpan HeartBeatInterval { get; set; }

        public void AddConnection(ITrackingDisconnect connection) {
            _connections.Remove(connection);
            _connections.Add(connection);
        }

        public void RemoveConnection(ITrackingDisconnect connection) {
            _connections.Remove(connection);
        }

        private void Beat() {
            try {
                Parallel.ForEach(_connections.GetSnapshot(), (connection) => {
                    if (!connection.IsAlive) {
                        connection.Disconnect();
                        _connections.Remove(connection);
                    }
                });
            }
            catch (Exception ex) {
                Trace.TraceError("SignalR error during transport heart beat: {0}", ex);
            }
        }

        private class ClientIdEqualityComparer : IEqualityComparer<ITrackingDisconnect> {
            public bool Equals(ITrackingDisconnect x, ITrackingDisconnect y) {
                return String.Equals(x.ClientId, y.ClientId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ITrackingDisconnect obj) {
                return obj.ClientId.GetHashCode();
            }
        }
    }
}