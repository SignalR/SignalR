using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace SignalR.ScaleOut
{
    /// <summary>
    /// A signaler that uses SQL Server and Query Notifications to send signals between app-domains
    /// </summary>
    public class SQLQueryNotificationsSignalBus : ISignalBus
    {
        private static readonly string _getSignalsSQL = "SELECT [SignalID], [EventKey], [LastSignaledAt] " +
                                                                "FROM {TableName} " +
                                                                "WHERE [LastSignaledAt] > @LastSignaledAt";

        private static readonly string _upsertSignalSQL = "MERGE dbo.[SignalRSignals] WITH (HOLDLOCK) AS s " +
                                                          "USING ( SELECT @EventKey AS EventKey ) AS existing_Signal " +
                                                          "      ON s.EventKey = existing_Signal.EventKey " +
                                                          "WHEN MATCHED THEN " +
                                                          "    UPDATE " +
                                                          "            SET s.LastSignaledAt = SYSDATETIME() " +
                                                          "WHEN NOT MATCHED THEN " +
                                                          "    INSERT ( EventKey, LastSignaledAt ) " +
                                                          "    VALUES ( @EventKey, SYSDATETIME() );";

        private DateTime _lastSignaledAt = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
        private bool _sqlDependencyListenerStarted = false;
        private object _ensureSqlDependencyListeningLocker = new object();
        private object _ensureSqlConnectionLocker = new object();

        public SQLQueryNotificationsSignalBus(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            ConnectionString = connectionString;
            SignalTableName = "[dbo].[SignalRSignals]";
            StartListeningForQueryNotification();
        }

        public virtual string ConnectionString { get; private set; }
        public virtual string SignalTableName { get; set; }

        public event EventHandler<SignaledEventArgs> Signaled;
        private void OnSignaled(string eventKey)
        {
            if (Signaled != null)
            {
                Signaled(this, new SignaledEventArgs(eventKey));
            }
        }

        public virtual Task Signal(string eventKey)
        {
            return InsertUpdateSignal(eventKey);

            // Maybe we can immediately raise event here too
            // and prevent same signal from SQL QN from propagating?
        }

        // TODO: Not sure this is even needed, or we can do it automatically
        public void StopSqlDependencyListener()
        {
            if (_sqlDependencyListenerStarted)
            {
                lock (_ensureSqlDependencyListeningLocker)
                {
                    if (_sqlDependencyListenerStarted)
                    {
                        SqlDependency.Stop(ConnectionString);
                    }
                }
            }
        }

        protected virtual Task InsertUpdateSignal(string eventKey)
        {
            var connection = CreateAndOpenConnection();
            var command = connection.CreateCommand();
            // UPSERT statement (http://weblogs.sqlteam.com/dang/archive/2009/01/31/UPSERT-Race-Condition-With-MERGE.aspx)
            command.CommandText = _upsertSignalSQL.Replace("{TableName}", SignalTableName);
            command.Parameters.AddWithValue("@EventKey", eventKey);
            return Task.Factory.FromAsync<int>(command.BeginExecuteNonQuery, command.EndExecuteNonQuery, null)
                .ContinueWith(_ => connection.Close());
        }

        private void StartListeningForQueryNotification()
        {
            EnsureSqlDependencyListening();
            var connection = CreateAndOpenConnection();
            var command = BuildQueryCommand(connection);
            var sd = new SqlDependency(command);
            sd.OnChange += (sender, e) =>
            {
                ProcessSignalReceived();
                StartListeningForQueryNotification();
            };
            Task.Factory.FromAsync<SqlDataReader>(command.BeginExecuteReader, command.EndExecuteReader, null)
                .ContinueWith(_ => connection.Close());
        }

        private void ProcessSignalReceived()
        {
            // Get event keys for signals since last signal received and update last signal ID
            var connection = CreateAndOpenConnection();
            var command = BuildQueryCommand(connection);
            var lastSignaledAtInResult = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            var eventKeys = new HashSet<string>();

            Task.Factory.FromAsync<SqlDataReader>(command.BeginExecuteReader, command.EndExecuteReader, null)
                .ContinueWith(t =>
                {
                    var rdr = t.Result;
                    while (rdr.Read())
                    {
                        //lastSignalIdInResult = rdr.GetInt32(0);
                        if (rdr.GetDateTime(2) > lastSignaledAtInResult)
                        {
                            lastSignaledAtInResult = rdr.GetDateTime(2);
                        }
                        eventKeys.Add(rdr.GetString(1));
                    }
                    rdr.Close();

                    foreach (var key in eventKeys)
                    {
                        OnSignaled(key);
                    }
                    _lastSignaledAt = lastSignaledAtInResult;
                    connection.Close();
                });
        }

        private SqlCommand BuildQueryCommand(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = _getSignalsSQL.Replace("{TableName}", SignalTableName);
            //command.Parameters.AddWithValue("LastSignalID", _lastSignalID);
            command.Parameters.AddWithValue("LastSignaledAt", _lastSignaledAt);
            return command;
        }

        private void EnsureSqlDependencyListening()
        {
            if (!_sqlDependencyListenerStarted)
            {
                lock (_ensureSqlDependencyListeningLocker)
                {
                    if (!_sqlDependencyListenerStarted)
                    {
                        SqlDependency.Start(ConnectionString);
                        var perm = new SqlClientPermission(PermissionState.Unrestricted);
                        perm.Demand();
                        _sqlDependencyListenerStarted = true;
                    }
                }
            }
        }

        private SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            Signaled += handler;
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            Signaled -= handler;
        }
    }
}