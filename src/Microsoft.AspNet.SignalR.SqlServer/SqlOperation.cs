// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class SqlOperation
    {
        public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(3);

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

            RetryDelay = DefaultRetryDelay;
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
            return ExecuteWithRetry(cmd => cmd.ExecuteScalar());
        }

        public int ExecuteNonQuery()
        {
            return ExecuteWithRetry(cmd => cmd.ExecuteNonQuery());
        }

        public SqlDataReader ExecuteReader()
        {
            return ExecuteWithRetry(cmd => cmd.ExecuteReader());
        }

        public Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            ExecuteWithRetry(cmd => cmd.ExecuteNonQueryAsync(), tcs);
            return tcs.Task;
        }

        private void ExecuteWithRetry<T>(Func<SqlCommand, Task<T>> commandFunc, TaskCompletionSource<T> tcs)
        {
            ExecuteWithRetry(commandFunc)
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
                    });
        }
        
        private T ExecuteWithRetry<T>(Func<SqlCommand, T> commandFunc)
        {
            SqlConnection connection = null;
            while (true)
            {
                try
                {
                    connection = new SqlConnection(_connectionString);
                    var command = CreateCommand(connection);
                    connection.Open();
                    return commandFunc(command);
                }
                catch (Exception ex)
                {
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
                finally
                {
                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                }
            }
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
