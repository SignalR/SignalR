﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    /// <summary>
    /// A DbOperation that continues to execute over and over as new results arrive.
    /// Will attempt to use SQL Query Notifications, otherwise falls back to a polling receive loop.
    /// </summary>
    internal class ObservableDbOperation : DbOperation, IDisposable, IDbBehavior
    {
        private readonly List<Tuple<int, int>> _updateLoopRetryDelays = new List<Tuple<int, int>> {
            new Tuple<int, int>(0, 3),      // 0ms x 3
            new Tuple<int, int>(10, 3),     // 10ms x 3
            new Tuple<int, int>(50, 2),     // 50ms x 2
            new Tuple<int, int>(100, 2),    // 100ms x 2
            new Tuple<int, int>(200, 2),    // 200ms x 2
            new Tuple<int, int>(1000, 2),  // 1000ms x 2
            new Tuple<int, int>(1500, 2),  // 1500ms x 2
            new Tuple<int, int>(3000, 1)   // 3000ms x 1
        };
        private readonly object _stopLocker = new object();
        private readonly ManualResetEventSlim _stopHandle = new ManualResetEventSlim(true);
        private readonly IDbBehavior _dbBehavior;

        private volatile bool _disposing;
        private long _notificationState;

        public ObservableDbOperation(string connectionString, string commandText, TraceSource traceSource, IDbProviderFactory dbProviderFactory, IDbBehavior dbBehavior, IDbExceptionBehavior dbExceptionBehavior)
            : base(connectionString, commandText, traceSource, dbProviderFactory, dbExceptionBehavior)
        {
            _dbBehavior = dbBehavior ?? this;
        }

        public ObservableDbOperation(string connectionString, string commandText, TraceSource traceSource, params SqlParameter[] parameters)
            : base(connectionString, commandText, traceSource, parameters)
        {
            _dbBehavior = this;
        }

        public Action<Exception> OnError { get; set; }

        public Action OnRetryLoopIteration { get; set; }

        /// <summary>
        /// Note this blocks the calling thread until an unrecoverable occurs or a SQL Query Notification can be set up
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Errors are reported via the callback")]
        public void ExecuteReaderWithUpdates(Action<IDataRecord, DbOperation> processRecord)
        {
            if (_disposing)
            {
                return;
            }

            lock (_stopLocker)
            {
                if (_disposing)
                {
                    return;
                }
                _stopHandle.Reset();
            }

            var useNotifications = false;

            try
            {
                useNotifications = _dbBehavior.StartSqlDependencyListener();
            }
            catch (Exception ex)
            {
                Stop(ex);
                return;
            }

            if (useNotifications)
            {
                _notificationState = NotificationState.ProcessingUpdates;
            }

            for (var i = 0; i < _dbBehavior.UpdateLoopRetryDelays.Count; i++)
            {
                var retry = _dbBehavior.UpdateLoopRetryDelays[i];
                var retryDelay = retry.Item1;
                var retryCount = retry.Item2;
                
                for (var j = 0; j < retryCount; j++)
                {
                    if (_disposing)
                    {
                        Stop(null);
                        return;
                    }

                    int recordCount;
                    try
                    {
                        if (OnRetryLoopIteration != null)
                        {
                            OnRetryLoopIteration();
                        }
                        recordCount = ExecuteReader(processRecord);
                    }
                    catch (Exception ex)
                    {
                        Stop(ex);
                        return;
                    }

                    if (recordCount > 0)
                    {
                        // We got records so start the retry loop again
                        i = -1;
                        break;
                    }

                    if (retryDelay > 0)
                    {
                        Trace.TraceVerbose("{0}Waiting {1}ms before checking for messages again", TracePrefix, retryDelay);

                        Thread.Sleep(retryDelay);
                    }

                    if (i == _dbBehavior.UpdateLoopRetryDelays.Count - 1 && j == retryCount - 1)
                    {
                        // Last retry loop iteration
                        if (!useNotifications)
                        {
                            // Last retry loop and we're not using notifications so just stay looping on the last retry delay
                            j = j - 1;
                        }
                        else
                        {
                            // No records after all retries, set up a SQL notification
                            try
                            {
                                ExecuteReader(processRecord, command =>
                                {
                                    _dbBehavior.AddSqlDependency(command, e => SqlDependency_OnChange(e, processRecord));
                                });

                                if (Interlocked.CompareExchange(ref _notificationState,
                                    NotificationState.AwaitingNotification, NotificationState.ProcessingUpdates) == NotificationState.NotificationReceived)
                                {
                                    // Updates were received while we were processing, start the receive loop again now
                                    i = -1;
                                    break; // break the inner for loop
                                };

                                lock (_stopLocker)
                                {
                                    if (_disposing)
                                    {
                                        _stopHandle.Set();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Stop(ex);
                            }
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Disposing")]
        public void Dispose()
        {
            lock (_stopLocker)
            {
                _disposing = true;
            }

            if (_notificationState != NotificationState.Disabled)
            {
                try
                {
                    SqlDependency.Stop(ConnectionString);
                }
                catch (Exception) { }
            }

            if (Interlocked.Read(ref _notificationState) == NotificationState.ProcessingUpdates)
            {
                _stopHandle.Wait();
            }
            _stopHandle.Dispose();
        }

        protected virtual void AddSqlDependency(IDbCommand command, Action<SqlNotificationEventArgs> callback)
        {
            command.AddSqlDependency(e => callback(e));
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread and we report exceptions asynchronously"),
         SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender", Justification = "Event handler")]
        protected virtual void SqlDependency_OnChange(SqlNotificationEventArgs e, Action<IDataRecord, DbOperation> processRecord)
        {
            lock (_stopLocker)
            {
                if (_disposing)
                {
                    return;
                }
            }

            if (Interlocked.CompareExchange(ref _notificationState,
                NotificationState.NotificationReceived, NotificationState.ProcessingUpdates) == NotificationState.ProcessingUpdates)
            {
                // New updates will be retreived by the original reader thread
                return;
            }

            // Check notification args for issues
            if (e.Type == SqlNotificationType.Change)
            {
                if (e.Info == SqlNotificationInfo.Insert
                    || e.Info == SqlNotificationInfo.Expired
                    || e.Info == SqlNotificationInfo.Resource)
                {
                    ExecuteReaderWithUpdates(processRecord);
                }
                else if (e.Info == SqlNotificationInfo.Restart)
                {
                    Trace.TraceWarning("{0}SQL Server restarting, starting buffering", TracePrefix);

                    if (OnRetry != null)
                    {
                        OnRetry(this);
                    }
                    ExecuteReaderWithUpdates(processRecord);
                }
                else if (e.Info == SqlNotificationInfo.Error)
                {
                    Trace.TraceWarning("{0}SQL notification error likely due to server becoming unavailable, starting buffering", TracePrefix);

                    if (OnRetry != null)
                    {
                        OnRetry(this);
                    }
                    ExecuteReaderWithUpdates(processRecord);
                }
                else
                {
                    // Fatal error, we don't expect to get here, end the receive loop

                    Trace.TraceError("{0}Unexpected SQL notification details: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);

                    Stop(new SqlMessageBusException(String.Format(CultureInfo.InvariantCulture, Resources.Error_UnexpectedSqlNotificationType, e.Type, e.Source, e.Info)));
                }
            }
            else if (e.Type == SqlNotificationType.Subscribe)
            {
                Debug.Assert(e.Info != SqlNotificationInfo.Invalid, "Ensure the SQL query meets the requirements for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx");

                Trace.TraceError("{0}SQL notification subscription error: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);

                if (e.Info == SqlNotificationInfo.TemplateLimit)
                {
                    // We've hit a subscription limit, pause for a bit then start again
                    if (RetryDelay.TotalMilliseconds > 0)
                    {
                        Thread.Sleep(RetryDelay);
                    }
                    ExecuteReaderWithUpdates(processRecord);
                }
                else
                {
                    // Unknown subscription error, let's stop using query notifications
                    _notificationState = NotificationState.Disabled;
                    try
                    {
                        SqlDependency.Stop(ConnectionString);
                    }
                    catch (Exception) { }

                    ExecuteReaderWithUpdates(processRecord);
                }
            }
        }

        protected virtual bool StartSqlDependencyListener()
        {
            lock (_stopLocker)
            {
                if (_disposing)
                {
                    return false;
                }
            }

            if (_notificationState == NotificationState.Disabled)
            {
                return false;
            }

            Trace.TraceVerbose("{0}: Starting SQL notification listener", TracePrefix);
            while (true)
            {
                try
                {
                    if (SqlDependency.Start(ConnectionString))
                    {
                        Trace.TraceVerbose("{0}SQL notificatoin listener started", TracePrefix);
                    }
                    else
                    {
                        Trace.TraceVerbose("{0}SQL notificatoin listener was already running", TracePrefix);
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    Trace.TraceInformation("{0}SQL Service Broker is disabled, disabling query notifications", TracePrefix);

                    _notificationState = NotificationState.Disabled;
                    return false;
                }
                catch (Exception ex)
                {
                    if (DbExceptionBehavior.IsRecoverableException(ex))
                    {
                        if (RetryDelay.TotalMilliseconds > 0)
                        {
                            Thread.Sleep(RetryDelay);
                        }
                    }
                    else
                    {
                        // Fatal error trying to start SQL notification listener
                        throw;
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Stopping is a terminal state on a bg thread")]
        protected virtual void Stop(Exception ex)
        {
            if (ex != null && OnError != null)
            {
                OnError(ex);
            }

            if (_notificationState != NotificationState.Disabled)
            {
                try
                {
                    SqlDependency.Stop(ConnectionString);
                }
                catch (Exception) { }
            }

            lock (_stopLocker)
            {
                if (_disposing)
                {
                    _stopHandle.Set();
                }
            }
        }

        private static class NotificationState
        {
            public const long Enabled = 0;
            public const long ProcessingUpdates = 1;
            public const long AwaitingNotification = 2;
            public const long NotificationReceived = 3;
            public const long Disabled = 4;
        }

        bool IDbBehavior.StartSqlDependencyListener()
        {
            return StartSqlDependencyListener();
        }

        IList<Tuple<int, int>> IDbBehavior.UpdateLoopRetryDelays
        {
            get { return _updateLoopRetryDelays; }
        }

        void IDbBehavior.AddSqlDependency(IDbCommand command, Action<SqlNotificationEventArgs> callback)
        {
            AddSqlDependency(command, callback);
        }
    }
}
