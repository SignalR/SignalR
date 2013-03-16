// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlOperation
    {
        private static readonly Action _noOp = () => { };

        private readonly string _connectionString;
        private readonly string _commandText;
        private readonly SqlParameter[] _parameters;
        private readonly Action _onRetry;

        public SqlOperation(string connectionString, string commandText)
            : this(connectionString, commandText, _noOp)
        {

        }

        public SqlOperation(string connectionString, string commandText, Action onRetry)
        {
            _connectionString = connectionString;
            _commandText = commandText;
            _onRetry = onRetry;

            RetryDelay = TimeSpan.FromSeconds(3);
        }

        public SqlOperation(string connectionString, string commandText, params SqlParameter[] parameters)
            : this(connectionString, commandText, _noOp, parameters)
        {

        }

        public SqlOperation(string connectionString, string commandText, Action onRetry, params SqlParameter[] parameters)
            : this(connectionString, commandText, onRetry)
        {
            _parameters = parameters;
        }

        public TimeSpan RetryDelay { get; set; }

        public object ExecuteScalar()
        {
            return Execute(cmd => cmd.ExecuteScalar());
        }

        public int ExecuteNonQuery()
        {
            return Execute(cmd => cmd.ExecuteNonQuery());
        }

        public int ExecuteReader(Action<SqlDataReader> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        public int ExecuteReader(Action<SqlDataReader> processRecord, Action<SqlNotificationEventArgs> onChange)
        {
            return Execute(cmd =>
            {
                if (onChange != null)
                {
                    // Setup SQL notification
                    var useNotifications = StartSqlDependencyListener();

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
            }, retryOnException: false);
        }

        public Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            ExecuteWithRetry(cmd => cmd.ExecuteNonQueryAsync(), tcs);
            return tcs.Task;
        }

        private T Execute<T>(Func<SqlCommand, T> commandFunc)
        {
            return Execute(commandFunc, true);
        }

        private T Execute<T>(Func<SqlCommand, T> commandFunc, bool retryOnException)
        {
            T result = default(T);
            SqlConnection connection = null;

            while (true)
            {
                try
                {
                    connection = new SqlConnection(_connectionString);
                    var command = CreateCommand(connection);
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
                            _onRetry();
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
            while (true)
            {
                try
                {
                    connection = new SqlConnection(_connectionString);
                    var command = CreateCommand(connection);

                    connection.Open();

                    commandFunc(command)
                        .Then(result => tcs.SetResult(result))
                        .Catch(ex =>
                        {
                            if (IsRecoverableException(ex.GetBaseException()))
                            {
                                _onRetry();
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
                        _onRetry();
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
                command.Parameters.AddRange(_parameters);
            }
            return command;
        }

        private bool StartSqlDependencyListener()
        {
            //TraceVerbose("Starting SQL notification listener");
            while (true)
            {
                try
                {
                    // TODO: Handle SqlExceptions here and retry, etc.
                    if (SqlDependency.Start(_connectionString))
                    {
                        //TraceVerbose("SQL notificatoin listener started");
                    }
                    else
                    {
                        //TraceVerbose("SQL notificatoin listener was already running");
                    }
                    return true;
                }
                catch (InvalidOperationException)
                {
                    //TraceInformation("SQL Service Broker is disabled, disabling query notifications");
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
