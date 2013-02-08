using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client
{
    public class HeartbeatMonitor : IDisposable
    {
#if !NETFX_CORE
        // Timer to determine when to notify the user and reconnect if required
        private Timer _timer;
#endif

        // Keep track of whether we have already disposed
        private bool _disposed;

        // Connection variable
        private readonly IConnection _connection;

        // To keep track of whether the user has been notified
        public bool HasBeenWarned { get; private set; }

        // To keep track of whether the client is already reconnecting
        public bool Reconnecting { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HeartBeatMonitor Class 
        /// </summary>
        /// <param name="connection"></param>
        public HeartbeatMonitor(IConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Updates LastKeepAlive and starts the timer
        /// </summary>
        public void Start()
        {
            _connection.UpdateLastKeepAlive();
            _disposed = false;
            HasBeenWarned = false;
            Reconnecting = false;
#if !NETFX_CORE
            _timer = new Timer(_ => Beat(), state: null, dueTime: _connection.KeepAliveData.CheckInterval, period: _connection.KeepAliveData.CheckInterval);
#endif
        }

        /// <summary>
        /// Callback function for the timer which determines if we need to notify the user or attempt to reconnect
        /// </summary>
#if NETFX_CORE
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Timer is not implemented on WinRT")]
#endif
        private void Beat()
        {
            TimeSpan timeElapsed = DateTime.UtcNow - _connection.KeepAliveData.LastKeepAlive;
            Beat(timeElapsed);
        }

        /// <summary>
        /// Logic to determine if we need to notify the user or attempt to reconnect
        /// </summary>
        /// <param name="timeElapsed"></param>
        public void Beat(TimeSpan timeElapsed)
        {
            if (_connection.State == ConnectionState.Connected)
            {
                if (timeElapsed >= _connection.KeepAliveData.Timeout)
                {
                    if (!Reconnecting)
                    {
                        // Connection has been lost
                        Debug.WriteLine("Connection Timed-out : Reconnecting {0}", DateTime.UtcNow);
                        Reconnecting = true;
                        _connection.Transport.LostConnection(_connection);
                    }
                }
                else if (timeElapsed >= _connection.KeepAliveData.TimeoutWarning)
                {
                    if (!HasBeenWarned)
                    {
                        // Inform user and set UserNotified to true
                        Debug.WriteLine("Connection Timeout Warning : Notifying user {0}", DateTime.UtcNow);
                        HasBeenWarned = true;
                        _connection.OnTimeoutWarning();
                    }
                }
                else
                {
                    HasBeenWarned = false;
                    Reconnecting = false;
                }
            }
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
            if (!_disposed)
            {
                if (disposing)
                {
#if !NETFX_CORE
                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }                    
#endif
                }

                // Indicate that the instance has been disposed
                _disposed = true;
            }
        }
    }
}
