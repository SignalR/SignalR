// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Transports;

#if NETFX_CORE
using System.Diagnostics.CodeAnalysis;
using Windows.System.Threading;
#endif

namespace Microsoft.AspNet.SignalR.Client
{
    public class HeartbeatMonitor : IDisposable
    {
#if !NETFX_CORE
        // Timer to determine when to notify the user and reconnect if required
        private Timer _timer;
#else
        private ThreadPoolTimer _timer;
#endif
        // Used to ensure that the Beat only executes when the connection is in the Connected state
        private readonly object _connectionStateLock;

        // Connection variable
        private readonly IConnection _connection;

        // How often to beat
        private readonly TimeSpan _beatInterval;

        // Whether to monitor the keep alive or not
        private bool _monitorKeepAlive;

        // To keep track of whether the user has been notified
        public bool HasBeenWarned { get; private set; }

        // To keep track of whether the client is already reconnecting
        public bool TimedOut { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HeartBeatMonitor Class 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectionStateLock"></param>
        /// <param name="beatInterval">How often to check connection status</param>
        public HeartbeatMonitor(IConnection connection, object connectionStateLock, TimeSpan beatInterval)
        {
            _connection = connection;
            _connectionStateLock = connectionStateLock;
            _beatInterval = beatInterval;
        }

        /// <summary>
        /// Starts the timer that triggers heartbeats  
        /// </summary>
        public void Start()
        {
            _monitorKeepAlive = _connection.KeepAliveData != null && _connection.Transport.SupportsKeepAlive;

            ClearFlags();
#if !NETFX_CORE
            _timer = new Timer(_ => Beat(), state: null, dueTime: _beatInterval, period: _beatInterval);
#else
            _timer = ThreadPoolTimer.CreatePeriodicTimer((timer) => Beat(), period: _beatInterval);
#endif
        }

        private void ClearFlags()
        {
            HasBeenWarned = false;
            TimedOut = false;
        }

        /// <summary>
        /// Callback function for the timer which determines if we need to notify the user or attempt to reconnect
        /// </summary>
#if NETFX_CORE
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Timer is not implemented on WinRT")]
#endif
        private void Beat()
        {
            var timeElapsed = DateTime.UtcNow - _connection.LastMessageAt;
            Beat(timeElapsed);
        }

        /// <summary>
        /// Logic to determine if we need to notify the user or attempt to reconnect
        /// </summary>
        /// <param name="timeElapsed"></param>
        public void Beat(TimeSpan timeElapsed)
        {
            if (_monitorKeepAlive)
            {
                CheckKeepAlive(timeElapsed);
            }

            _connection.MarkActive();
        }

        private void CheckKeepAlive(TimeSpan timeElapsed)
        {
            // Flags used to detect what kind of follow-up action should be taken.
            var connectionSlow = false;
            IClientTransport transport = null;

            lock (_connectionStateLock)
            {
                if (_connection.State == ConnectionState.Connected)
                {
                    if (timeElapsed >= _connection.KeepAliveData.Timeout)
                    {
                        if (!TimedOut)
                        {
                            // Connection has been lost
                            _connection.Trace(TraceLevels.Events, "Connection Timed-out : Transport Lost Connection");
                            TimedOut = true;

                            // Capture the transport to call LostConnection on it after the lock ends.
                            // We capture it locally because _connection.Transport may be re-initialized before we call LostConnection
                            // and we don't want to call LostConnection on the re-initialized transport.
                            transport = _connection.Transport;
                        }
                    }
                    else if (timeElapsed >= _connection.KeepAliveData.TimeoutWarning)
                    {
                        if (!HasBeenWarned)
                        {
                            // Inform user and set HasBeenWarned to true
                            _connection.Trace(TraceLevels.Events, "Connection Timeout Warning : Notifying user");
                            HasBeenWarned = true;

                            // We don't want to run the user event "ConnectionSlow" in the lock.
                            connectionSlow = true;
                        }
                    }
                    else
                    {
                        ClearFlags();
                    }
                }
            }

            // If there's a non-null 'transport' value, then it means the connection timed out and we need to call LostConnection.
            if (transport != null)
            {
                transport.LostConnection(_connection);
            }

            if (connectionSlow)
            {
                _connection.OnConnectionSlow();
            }
        }

        //virtual for testing
        internal virtual void Reconnected()
        {
            ClearFlags();
        }

        /// <summary>
        /// Dispose off the timer
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose off the timer
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                {
#if !NETFX_CORE

                    _timer.Dispose();
                    _timer = null;
#else
                    _timer.Cancel();
                    _timer = null;
#endif
                }

            }
        }
    }
}

