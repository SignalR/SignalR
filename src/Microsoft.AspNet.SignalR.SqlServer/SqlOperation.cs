// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    // TODO: Should we make this IDisposable and stop any in progress reader loops/notifications on Dispose?
    internal class SqlOperation
    {
        private static readonly Action<object> _noOp = _ => { };

        private readonly string _connectionString;
        private readonly string _commandText;
        private readonly Action<SqlOperation> _onRetry;
        private readonly Action<Exception> _onError;
        private readonly TraceSource _trace;
        private readonly int[] _updateLoopRetryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200, 1000, 1500, 3000 };
        private readonly ManualResetEventSlim _mre = new ManualResetEventSlim();

        private List<SqlParameter> _parameters = new List<SqlParameter>();
        private bool _useQueryNotifications = true;

        public SqlOperation(string connectionString, string commandText, TraceSource traceSource)
            : this(connectionString, commandText, _noOp, _noOp, traceSource)
        {

        }

        public SqlOperation(string connectionString, string commandText, Action<SqlOperation> onRetry, Action<Exception> onError, TraceSource traceSource)
        {
            _connectionString = connectionString;
            _commandText = commandText;
            _onRetry = onRetry;
            _onError = onError;
            _trace = traceSource;

            RetryDelay = TimeSpan.FromSeconds(3);
        }

        public SqlOperation(string connectionString, string commandText, TraceSource traceSource, params SqlParameter[] parameters)
            : this(connectionString, commandText, _noOp, _noOp, traceSource, parameters)
        {

        }

        public SqlOperation(string connectionString, string commandText, Action<SqlOperation> onRetry, Action<Exception> onError, TraceSource traceSource, params SqlParameter[] parameters)
            : this(connectionString, commandText, onRetry, onError, traceSource)
        {
            _parameters.AddRange(parameters);
        }

        public string TracePrefix { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public IList<SqlParameter> Parameters
        {
            get { return _parameters; }
        }

        public object ExecuteScalar()
        {
            return Execute(cmd => cmd.ExecuteScalar());
        }

        public int ExecuteNonQuery()
        {
            return Execute(cmd => cmd.ExecuteNonQuery());
        }

        public Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            ExecuteWithRetry(cmd => cmd.ExecuteNonQueryAsync(), tcs);
            return tcs.Task;
        }

        public int ExecuteReader(Action<SqlDataReader> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        private int ExecuteReader(Action<SqlDataReader> processRecord, Action<SqlCommand> commandAction)
        {
            return Execute(cmd =>
            {
                if (commandAction != null)
                {
                    commandAction(cmd);
                }

                var reader = cmd.ExecuteReader();
                var count = 0;

                if (!reader.HasRows)
                {
                    return count;
                }

                while (reader.Read())
                {
                    count++;
                    processRecord(reader);
                }

                return count;
            });
        }

        public void ExecuteReaderWithUpdates(Action<SqlDataReader> processRecord)
        {
            var useNotifications = StartSqlDependencyListener();

            for (var i = 0; i < _updateLoopRetryDelays.Length; i++)
            {
                int recordCount;
                try
                {
                    recordCount = ExecuteReader(processRecord);
                }
                catch (Exception)
                {
                    if (useNotifications)
                    {
                        SqlDependency.Stop(_connectionString);
                    }
                    throw;
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
                    _trace.TraceVerbose("{0}Waiting {1}ms before checking for messages again", TracePrefix, retryDelay);

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
            catch (Exception)
            {
                SqlDependency.Stop(_connectionString);
                throw;
            }
        }

        private void SqlDependency_OnChange(object sender, SqlNotificationEventArgs e, Action<SqlDataReader> processRecord)
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
                    _trace.TraceWarning("{0}SQL Server restarting, starting buffering", TracePrefix);

                    _onRetry(this);
                    ExecuteReaderWithUpdates(processRecord);
                }
                else if (e.Info == SqlNotificationInfo.Error)
                {
                    _trace.TraceWarning("{0}SQL notification error likely due to server becoming unavailable, starting buffering", TracePrefix);

                    _onRetry(this);
                    ExecuteReaderWithUpdates(processRecord);
                }
                else
                {
                    _trace.TraceError("{0}Unexpected SQL notification details: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);
                    
                    _onError(new SqlMessageBusException(String.Format(CultureInfo.InvariantCulture, Resources.Error_UnexpectedSqlNotificationType, e.Type, e.Source, e.Info)));
                }
            }
            else if (e.Type == SqlNotificationType.Subscribe)
            {
                Debug.Assert(e.Info != SqlNotificationInfo.Invalid, "Ensure the SQL query meets the requirements for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx");

                _trace.TraceError("{0}SQL notification subscription error: Type={1}, Source={2}, Info={3}", TracePrefix, e.Type, e.Source, e.Info);

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
                        SqlDependency.Stop(_connectionString);
                    }
                    catch (Exception) { }

                    ExecuteReaderWithUpdates(processRecord);
                }
            }
        }

        private T Execute<T>(Func<SqlCommand, T> commandFunc)
        {
            return Execute(commandFunc, true);
        }

        private T Execute<T>(Func<SqlCommand, T> commandFunc, bool retryOnException)
        {
            T result = default(T);
            SqlConnection connection = null;
            SqlCommand command = null;
            while (true)
            {
                try
                {
                    connection = new SqlConnection(_connectionString);
                    command = CreateCommand(connection);
                    connection.Open();
                    result = commandFunc(command);
                    break;
                }
                catch (Exception ex)
                {
                    if (IsRecoverableException(ex))
                    {
                        if (retryOnException)
                        {
                            _onRetry(this);
                            Thread.Sleep(RetryDelay);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                }
            }

            return result;
        }

        private void ExecuteWithRetry<T>(Func<SqlCommand, Task<T>> commandFunc, TaskCompletionSource<T> tcs)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            while (true)
            {
                try
                {
                    connection = new SqlConnection(_connectionString);
                    command = CreateCommand(connection);

                    connection.Open();

                    commandFunc(command)
                        .Then(result => tcs.SetResult(result))
                        .Catch(ex =>
                        {
                            if (IsRecoverableException(ex.GetBaseException()))
                            {
                                _onRetry(this);
                                TaskAsyncHelper.Delay(RetryDelay)
                                               .Then(() => ExecuteWithRetry(commandFunc, tcs));
                            }
                            else
                            {
                                tcs.SetUnwrappedException(ex);
                            }
                        })
                        .Finally(state =>
                        {
                            var conn = (SqlConnection)state;
                            if (conn != null)
                            {
                                conn.Dispose();
                            }
                        }, connection);
                    
                    break;
                }
                catch (Exception ex)
                {
                    if (connection != null)
                    {
                        connection.Dispose();
                    }

                    if (IsRecoverableException(ex))
                    {
                        _onRetry(this);
                        Thread.Sleep(RetryDelay);
                    }
                    else
                    {
                        throw;
                    }
                }
            };
        }

        private SqlCommand CreateCommand(SqlConnection connection)
        {   
            var command = new SqlCommand(_commandText, connection);
            if (_parameters != null)
            {
                for (var i = 0; i < _parameters.Count; i++)
                {
                    var sourceParameter = _parameters[i];
                    var newParameter = new SqlParameter
                    {
                        ParameterName = sourceParameter.ParameterName,
                        SqlDbType = sourceParameter.SqlDbType,
                        SqlValue = sourceParameter.SqlValue,
                        Direction = sourceParameter.Direction,
                        IsNullable = sourceParameter.IsNullable
                    };
                    Parameters.Add(newParameter);
                }
            }
            return command;
        }

        private bool StartSqlDependencyListener()
        {
            if (!_useQueryNotifications)
            {
                return false;
            }

            _trace.TraceVerbose("{0}: Starting SQL notification listener", TracePrefix);
            while (true)
            {
                try
                {
                    if (SqlDependency.Start(_connectionString))
                    {
                        _trace.TraceVerbose("{0}SQL notificatoin listener started", TracePrefix);
                    }
                    else
                    {
                        _trace.TraceVerbose("{0}SQL notificatoin listener was already running", TracePrefix);
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    _trace.TraceInformation("{0}SQL Service Broker is disabled, disabling query notifications", TracePrefix);
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

        private static bool IsRecoverableException(Exception exception)
        {
            var sqlException = exception as SqlException;

            return
                (exception is InvalidOperationException &&
                    String.Equals(exception.Source, "System.Data", StringComparison.OrdinalIgnoreCase) &&
                    exception.Message.StartsWith("Timeout expired", StringComparison.OrdinalIgnoreCase))
                ||
                (sqlException != null && (
                    sqlException.Number == SqlErrorNumbers.ConnectionTimeout ||
                    sqlException.Number == SqlErrorNumbers.ServerNotFound ||
                    sqlException.Number == SqlErrorNumbers.TransportLevelError ||
                // Failed to start a MARS session due to the server being unavailable
                    (sqlException.Number == SqlErrorNumbers.Unknown && sqlException.Message.IndexOf("error: 19", StringComparison.OrdinalIgnoreCase) >= 0)
                )
            );
        }

        private static class SqlErrorNumbers
        {
            // SQL error numbers: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlerror.number.aspx/html
            public const int Unknown = -1;
            public const int ConnectionTimeout = -2;
            public const int ServerNotFound = 2;
            public const int TransportLevelError = 233;
        }
    }
}
