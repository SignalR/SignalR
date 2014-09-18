// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal sealed class TaskQueueMonitor : ITaskMonitor, IDisposable
    {
#if !NETFX_CORE
        private Timer _timer;
#else
        private ThreadPoolTimer _timer;
#endif

        private readonly IConnection _connection;
        private readonly TimeSpan _deadlockErrorTimeout;
 
        // The use of any of the fields below should be synchronized using _lockObj
        private readonly object _lockObj = new object();

        private uint _currTaskId;
        private uint _prevTaskId;

        private bool _isTaskRunning;
        private bool _errorRaised;

        public TaskQueueMonitor(IConnection connection, TimeSpan deadlockErrorTimeout)
        {
            _connection = connection;
            _deadlockErrorTimeout = deadlockErrorTimeout;

#if !NETFX_CORE
            _timer = new Timer(_ => Beat(), state: null, dueTime: deadlockErrorTimeout, period: deadlockErrorTimeout);
#else
            _timer = ThreadPoolTimer.CreatePeriodicTimer(_ => Beat(), period: deadlockErrorTimeout);
#endif
        }

        public void TaskStarted()
        {
            lock (_lockObj)
            {
                Debug.Assert(!_isTaskRunning, "A task has already started. Only one task can be running at a time.");

                _errorRaised = false;
                _isTaskRunning = true;
                _currTaskId++;
            }
        }

        public void TaskCompleted()
        {
            lock (_lockObj)
            {
                Debug.Assert(_isTaskRunning, "No task is currently running to mark as completed.");

                _isTaskRunning = false;
            }
        }

        //Internal for testing purposes
        internal void Beat()
        {
            lock (_lockObj)
            {
                if (!_errorRaised && _isTaskRunning && _currTaskId == _prevTaskId)
                {
                    var errorMessage = String.Format(CultureInfo.CurrentCulture,
                                                     Resources.Error_PossibleDeadlockDetected,
                                                     _deadlockErrorTimeout.TotalSeconds);

                    _connection.OnError(new SlowCallbackException(errorMessage));

                    _errorRaised = true;
                }

                _prevTaskId = _currTaskId;
            }
        }

        /// <summary>
        /// Dispose off the timer
        /// </summary>
        public void Dispose()
        {
            var timer = _timer;

            if (timer != null)
            {
#if !NETFX_CORE
                timer.Dispose();
#else
                timer.Cancel();
#endif
            }

            _timer = null;
        }
    }
}
