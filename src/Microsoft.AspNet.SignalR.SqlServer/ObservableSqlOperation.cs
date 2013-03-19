// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    // TODO: Should we make this IDisposable and stop any in progress reader loops/notifications on Dispose?
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Needs review")]
    internal class ObservableSqlOperation : SqlOperation
    {
        private readonly int[] _updateLoopRetryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200, 1000, 1500, 3000 };
        private readonly ManualResetEventSlim _mre = new ManualResetEventSlim();

        private bool _useQueryNotifications = true;

        public ObservableSqlOperation(string connectionString, string commandText, TraceSource traceSource, params SqlParameter[] parameters)
            : base(connectionString, commandText, traceSource, parameters)
        {
            
        }

        public Action<Exception> OnError { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Errors are reported via the callback")]
        public void ExecuteReaderWithUpdates(Action<SqlDataReader, SqlOperation> processRecord)
        {
            var useNotifications = StartSqlDependencyListener();

            for (var i = 0; i < _updateLoopRetryDelays.Length; i++)
            {
                int recordCount;
                try
                {
                    recordCount = ExecuteReader(processRecord);
                }
                catch (Exception ex)
                {
                    if (useNotifications)
                    {
                        SqlDependency.Stop(ConnectionString);
                    }
                    if (OnError != null)
                    {
                        OnError(ex);
                    }
                    return;
                }

                if (recordCount > 0)
                {
                    // We got records so reset the retry delay index
                    i = -1;
                    continue;
                }

                var retryDelay = _updateLoopRetryDelays[i];
                if (retryDelay > 0)
                {
                    Trace.TraceVerbose("{0}Waiting {1}ms before checking for messages again", TracePrefix, retryDelay);

                    Thread.Sleep(retryDelay);
                }

                if (i == _updateLoopRetryDelays.Length - 1 && !useNotifications)
                {
                    // Not using notifications so just stay looping on the last retry delay
                    i = i - 1;
                }
            }

            // No records after all retries, set up a SQL notification
            try
            {
                // We need to ensure that the following ExecuteReader call completes before the 
                // SqlDependency OnChange handler runs, otherwise we could have two readers being
                // processed concurrently.
                _mre.Reset();
                ExecuteReader(processRecord, command =>
                {
                    var dependency = new SqlDependency(command);
                    dependency.OnChange += (s, e) => SqlDependency_OnChange(s, e, processRecord);
                });
                _mre.Set();
            }
            catch (Exception ex)
            {
                SqlDependency.Stop(ConnectionString);
                if (OnError != null)
                {
                    OnError(ex);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread and we report exceptions asynchronously"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender", Justification = "Event handler")]
        private void SqlDependency_OnChange(object sender, SqlNotificationEventArgs e, Action<SqlDataReader, SqlOperation> processRecord)
        {
            // TODO: Could we do this without blocking with some fancy Interlocked gymnastics?
            _mre.Wait();

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
                    Trace.TraceError("{0}Unexpected SQL notification details: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);
                    if (OnError != null)
                    {
                        OnError(new SqlMessageBusException(String.Format(CultureInfo.InvariantCulture, Resources.Error_UnexpectedSqlNotificationType, e.Type, e.Source, e.Info)));
                    }
                }
            }
            else if (e.Type == SqlNotificationType.Subscribe)
            {
                Debug.Assert(e.Info != SqlNotificationInfo.Invalid, "Ensure the SQL query meets the requirements for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx");

                Trace.TraceError("{0}SQL notification subscription error: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);

                if (e.Info == SqlNotificationInfo.TemplateLimit)
                {
                    // We've hit a subscription limit, pause for a bit then start again
                    Thread.Sleep(RetryDelay);
                    ExecuteReaderWithUpdates(processRecord);
                }
                else
                {
                    // Unknown subscription error, let's stop using query notifications
                    _useQueryNotifications = false;
                    try
                    {
                        SqlDependency.Stop(ConnectionString);
                    }
                    catch (Exception) { }

                    ExecuteReaderWithUpdates(processRecord);
                }
            }
        }

        private bool StartSqlDependencyListener()
        {
            if (!_useQueryNotifications)
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
                    return false;
                }
                catch (Exception ex)
                {
                    if (IsRecoverableException(ex))
                    {
                        Thread.Sleep(RetryDelay);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
