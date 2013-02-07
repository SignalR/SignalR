using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client
{
    public class HeartbeatMonitor : IDisposable
    {
        // Timer to determine when to notify the user and reconnect if required
        private Timer _timer;

        // Keep track of whether we have already disposed
        private bool _disposed;

        // Connection variable
        private IConnection _connection;

        // To keep track of whether the user has been notified
        public bool UserNotified { get; private set; }

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
            UserNotified = false;
            Reconnecting = false;
            _timer = new Timer(_ => Beat(), state: null, dueTime: _connection.KeepAliveData.CheckInterval, period: _connection.KeepAliveData.CheckInterval);
        }

        /// <summary>
        /// Callback function for the timer which determines if we need to notify the user or attempt to reconnect
        /// </summary>
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
                    if (!UserNotified)
                    {
                        // Inform user and set UserNotified to true
                        Debug.WriteLine("Connection Timeout Warning : Notifying user {0}", DateTime.UtcNow);
                        UserNotified = true;
                        _connection.OnTimeoutWarning();
                    }
                }
                else
                {
                    UserNotified = false;
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
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }
                }

                // Indicate that the instance has been disposed
                _disposed = true;
                _timer = null;
            }
        }
    }
}
