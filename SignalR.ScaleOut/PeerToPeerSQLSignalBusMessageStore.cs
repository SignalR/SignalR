using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;
using SignalR.Web;

namespace SignalR.ScaleOut
{
    public class PeerToPeerSQLSignalBusMessageStore : IMessageStore, ISignalBus
    {
        private static readonly object _peerDiscoveryLocker = new object();

        private const string _getMessageIdSql = "INSERT INTO {TableName} (EventKey, Created)" +
                                                "VALUES (@EventKey, GETDATE()) " +
                                                "SELECT @@IDENTITY";

        private InProcessMessageStore _store = new InProcessMessageStore();
        private InProcessSignalBus _signalBus = new InProcessSignalBus();
        private readonly List<string> _peers = new List<string>();
        private bool _peersDiscovered = false;
        private string _connectionString;

        public PeerToPeerSQLSignalBusMessageStore(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            _connectionString = connectionString;
            MessageTableName = "[dbo].[SignalRMessages]";
            Id = Guid.NewGuid();
        }

        protected internal Guid Id { get; private set; }

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

        private IPeerUrlSource PeerUrlSource
        {
            get
            {
                var source = DependencyResolver.Resolve<IPeerUrlSource>();
                if (source == null)
                {
                    throw new InvalidOperationException("No implementation of IPeerUrlSource is registered.");
                }
                return source;
            }
        }

        public string MessageTableName { get; set; }

        public Task<long?> GetLastId()
        {
            return _store.GetLastId();
        }

        public Task Save(string key, object value)
        {
            // Save it locally then broadcast to other peers
            return GetMessageId(key)
                .Success(idTask =>
                {
                    var message = new Message(key, idTask.Result, value);
                    return Task.Factory.ContinueWhenAll(new[] {
                            _store.Save(message),
                            SendMessageToPeers(message)
                        },
                        _ => { });
                })
                .Unwrap();
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            return _store.GetAllSince(key, id);
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            _signalBus.AddHandler(eventKey, handler);
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            _signalBus.RemoveHandler(eventKey, handler);
        }

        public Task Signal(string eventKey)
        {
            // We only signal locally, peers were self-signaled when the message was sent
            return _signalBus.Signal(eventKey);
        }

        protected internal Task MessageReceived(string payload)
        {
            // Parse the payload into a message object, save to the store and signal the local bus
            var message = Json.Parse<WireMessage>(payload).ToMessage();
            return message != null
                ? _store.Save(message)
                    .Success(t => _signalBus.Signal(message.SignalKey))
                    .Unwrap()
                : TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Override this method to prepare the request before it is sent to peers, e.g. to add authentication credentials
        /// </summary>
        /// <param name="request">The request being sent to peers</param>
        protected virtual void PrepareRequest(WebRequest request) { }

        private Task<long> GetMessageId(string key)
        {
            var connection = CreateAndOpenConnection();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
            var cmd = new SqlCommand(_getMessageIdSql.Replace("{TableName}", MessageTableName), connection, transaction);
            cmd.Parameters.AddWithValue("EventKey", key);
            return cmd.ExecuteScalarAsync<long>()
                .ContinueWith(idTask =>
                {
                    // We purposely don't commit the transaction, we just wanted the ID anyway, not the record
                    connection.Close();
                    return idTask;
                })
                .Unwrap();
        }

        private Task SendMessageToPeers(Message message)
        {
            EnsurePeersDiscovered();

            if (!_peers.Any())
            {
                return TaskAsyncHelper.Empty;
            }

            // Loop through peers and send the message
            var queryString = "?" + PeerToPeerHelper.RequestKeys.EventKey + "=" + HttpUtility.UrlEncode(message.SignalKey);
            var peerCallTasks = _peers.Select(peer =>
            {
                var data = new Dictionary<string, string> {
                        { PeerToPeerHelper.RequestKeys.Message, Json.Stringify(message) }
                    };
                return HttpHelper.PostAsync(peer + MessageReceiverHandler.HandlerName + queryString, PrepareRequest, data)
                    .ContinueWith(requestTask =>
                    {
                        if (requestTask.Status == TaskStatus.RanToCompletion)
                        {
                            requestTask.Result.Close();
                        }
                    });
            }).ToArray();

            // REVIEW: We are waiting on peer sending here, and faulting if any fault. Not sure if that's what we want or not.
            return peerCallTasks.AllSucceeded(() => { });
        }

        private void EnsurePeersDiscovered()
        {
            PeerToPeerHelper.EnsurePeersDiscovered(ref _peersDiscovered, PeerUrlSource, _peers, MessageReceiverHandler.HandlerName, Id, _peerDiscoveryLocker, PrepareRequest);
        }

        private SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }


        public Task<IOrderedEnumerable<Message>> GetAllSince(IEnumerable<string> keys, long id)
        {
            throw new NotImplementedException();
        }
    }

    public class WireMessage
    {
        public string SignalKey { get; set; }
        public object Value { get; set; }
        public long Id { get; set; }
        public DateTime Created { get; set; }

        public Message ToMessage()
        {
            return new Message(SignalKey, Id, Value, Created);
        }
    }
}