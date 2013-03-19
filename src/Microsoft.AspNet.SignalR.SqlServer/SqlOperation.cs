// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    // TODO: Should we make this IDisposable and stop any in progress reader loops/notifications on Dispose?
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Needs review")]
    internal class SqlOperation
    {
        private List<SqlParameter> _parameters = new List<SqlParameter>();

        public SqlOperation(string connectionString, string commandText, TraceSource traceSource)
        {
            ConnectionString = connectionString;
            CommandText = commandText;
            Trace = traceSource;

            RetryDelay = TimeSpan.FromSeconds(3);
        }

        public SqlOperation(string connectionString, string commandText, TraceSource traceSource, params SqlParameter[] parameters)
            : this(connectionString, commandText, traceSource)
        {
            if (parameters != null)
            {
                _parameters.AddRange(parameters);
            }
        }

        public string TracePrefix { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public Action<SqlOperation> OnRetry { get; set; }

        public IList<SqlParameter> Parameters
        {
            get { return _parameters; }
        }

        protected TraceSource Trace { get; private set; }

        protected string ConnectionString { get; private set; }

        protected string CommandText { get; private set; }

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

        public int ExecuteReader(Action<SqlDataReader, SqlOperation> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        protected int ExecuteReader(Action<SqlDataReader, SqlOperation> processRecord, Action<SqlCommand> commandAction)
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
                    processRecord(reader, this);
                }

                return count;
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It's the caller's responsibility to dispose as the command is returned"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "General purpose SQL utility command")]
        protected SqlCommand CreateCommand(SqlConnection connection)
        {
            var command = new SqlCommand(CommandText, connection);
            if (Parameters != null && Parameters.Count > 0)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    var sourceParameter = Parameters[i];
                    var newParameter = new SqlParameter
                    {
                        ParameterName = sourceParameter.ParameterName,
                        SqlDbType = sourceParameter.SqlDbType,
                        SqlValue = sourceParameter.SqlValue,
                        Direction = sourceParameter.Direction,
                        IsNullable = sourceParameter.IsNullable
                    };
                    command.Parameters.Add(newParameter);
                }
            }
            return command;
        }

        private T Execute<T>(Func<SqlCommand, T> commandFunc)
        {
            return Execute(commandFunc, true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "False positive?")]
        private T Execute<T>(Func<SqlCommand, T> commandFunc, bool retryOnException)
        {
            T result = default(T);
            SqlConnection connection = null;
            SqlCommand command = null;
            while (true)
            {
                try
                {
                    connection = new SqlConnection(ConnectionString);
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
                            if (OnRetry != null)
                            {
                                OnRetry(this);
                            }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Disposed in async Finally block"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in async Finally block")]
        private void ExecuteWithRetry<T>(Func<SqlCommand, Task<T>> commandFunc, TaskCompletionSource<T> tcs)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            while (true)
            {
                try
                {
                    connection = new SqlConnection(ConnectionString);
                    command = CreateCommand(connection);

                    connection.Open();

                    commandFunc(command)
                        .Then(result => tcs.SetResult(result))
                        .Catch(ex =>
                        {
                            if (IsRecoverableException(ex.GetBaseException()))
                            {
                                if (OnRetry != null)
                                {
                                    OnRetry(this);
                                }
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
                        if (OnRetry != null)
                        {
                            OnRetry(this);
                        }
                        Thread.Sleep(RetryDelay);
                    }
                    else
                    {
                        throw;
                    }
                }
            };
        }

        protected static bool IsRecoverableException(Exception exception)
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
