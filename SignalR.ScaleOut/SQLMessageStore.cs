using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.ScaleOut
{
    public class SQLMessageStore : IMessageStore
    {
        private static readonly string _getLastIdSQL = "SELECT MAX([MessageId]) FROM {TableName}";

        private static readonly string _saveSQL = "INSERT INTO {TableName} (EventKey, SmallValue, BigValue, Created) " +
                                                  "VALUES (@EventKey, @SmallValue, @BigValue, GETDATE())";

        private static readonly string _getAllSQL = "SELECT [MessageId], COALESCE([SmallValue],[BigValue]) as [Value], [Created], [EventKey] " +
                                                    "FROM {TableName} " +
                                                    "WHERE [EventKey] = @EventKey ";

        private static readonly string _getAllSinceSQL = _getAllSQL +
                                                         "AND [MessageId] > @MessageId";

        //private static readonly string _getAllSinceMultiEventKeysSQL = "SELECT [MessageId], COALESCE([SmallValue],[BigValue]) as [Value], [Created], [EventKey] " +
        //                                                               "FROM {TableName} m " +
        //                                                               "    INNER JOIN [dbo].[SignalR_charlist_to_table](@EventKey, ',') k " +
        //                                                               "        ON m.[EventKey] = k.[nstr] " +
        //                                                               "WHERE m.[MessageId] > @MessageId";

        // Interval to wait before cleaning up old queries
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private readonly ConcurrentDictionary<Tuple<long, string>, Task<IEnumerable<Message>>> _queries = new ConcurrentDictionary<Tuple<long, string>, Task<IEnumerable<Message>>>();

        private readonly Timer _timer;

        public SQLMessageStore(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            ConnectionString = connectionString;
            MessageTableName = "[dbo].[SignalRMessages]";

            _timer = new Timer(RemoveOldQueries, null, _cleanupInterval, _cleanupInterval);
        }

        private IJsonSerializer Json
        {
            get
            {
                var json = DependencyResolver.Resolve<IJsonSerializer>();
                if (json == null)
                {
                    throw new InvalidOperationException("No implementation of IJsonSerializer is registered.");
                }
                return json;
            }
        }

        public virtual string ConnectionString { get; private set; }
        public virtual string MessageTableName { get; set; }

        public Task<long?> GetLastId()
        {
            var connection = CreateAndOpenConnection();
            var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            var cmd = new SqlCommand(_getLastIdSQL.Replace("{TableName}", MessageTableName), connection, transaction);

            return cmd.ExecuteScalarAsync<long?>()
                .ContinueWith(t =>
                {
                    connection.Close();
                    return t.Result;
                });
        }

        public Task Save(string key, object value)
        {
            var connection = CreateAndOpenConnection();
            var cmd = new SqlCommand(_saveSQL.Replace("{TableName}", MessageTableName), connection);
            var json = Json.Stringify(value);
            cmd.Parameters.AddWithValue("EventKey", key);
            if (json.Length <= 2000)
            {
                cmd.Parameters.AddWithValue("SmallValue", json);
                cmd.Parameters.AddWithValue("BigValue", DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("SmallValue", DBNull.Value);
                cmd.Parameters.AddWithValue("BigValue", json);
            }
            return cmd.ExecuteNonQueryAsync()
                .ContinueWith(t =>
                {
                    connection.Close();
                });
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            return _queries.GetOrAdd(Tuple.Create(id, key),
                GetMessages(key, _getAllSinceSQL.Replace("{TableName}", MessageTableName),
                    new[] {
                        new SqlParameter("EventKey", key),
                        new SqlParameter("MessageId", id)
                    }
                ).ContinueWith(t =>
                {
                    if (t.Exception != null || !t.Result.Any())
                    {
                        // Remove from queries
                        Task<IEnumerable<Message>> removedQuery;
                        _queries.TryRemove(Tuple.Create(id, key), out removedQuery);
                    }
                    return t.Result;
                })
            );
        }

        private Task<IEnumerable<Message>> GetMessages(string key, string sql, SqlParameter[] parameters)
        {
            var connection = CreateAndOpenConnection();
            var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteReaderAsync()
                .ContinueWith<IEnumerable<Message>>(t =>
                {
                    var rdr = t.Result;
                    var messages = new List<Message>();
                    while (rdr.Read())
                    {
                        messages.Add(new Message(
                            signalKey: key,
                            id: rdr.GetInt64(0),
                            value: Json.Parse(rdr.GetString(1)),
                            created: rdr.GetDateTime(2)
                        ));
                    }
                    connection.Close();
                    return messages;
                });
        }

        private SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private void RemoveOldQueries(object state)
        {
            // Take a snapshot of the queries
            var queries = _queries.ToList();

            // Remove all the expired ones
            foreach (var query in queries)
            {
                if (query.Value.IsCompleted)
                {
                    if (query.Value.Result.All(m => m.Expired))
                    {
                        Task<IEnumerable<Message>> removed;
                        _queries.TryRemove(query.Key, out removed);
                    }
                }
            }
        }


        public Task<IOrderedEnumerable<Message>> GetAllSince(IEnumerable<string> keys, long id)
        {
            throw new NotImplementedException();
        }
    }
}