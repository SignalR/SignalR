using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;
using SignalR.SignalBuses;

namespace SignalR {
    public class PeerToPeerSQLSignalBusMessageStore : IMessageStore, ISignalBus {
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
            : this(connectionString, DependencyResolver.Resolve<IPeerUrlSource>()) {    
        }

        public PeerToPeerSQLSignalBusMessageStore(string connectionString, IPeerUrlSource peerUrlSource)
            : this(connectionString, peerUrlSource.GetPeerUrls()) {
        }

        public PeerToPeerSQLSignalBusMessageStore(string connectionString, IEnumerable<string> peers) {
            if (String.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException("connectionString");
            }

            _connectionString = connectionString;
            MessageTableName = "[dbo].[SignalRMessages]";
            _peers.AddRange(peers);
            Id = Guid.NewGuid();
        }

        protected internal Guid Id { get; private set; }

        private IJsonSerializer Json {
            get {
                var json = DependencyResolver.Resolve<IJsonSerializer>();
                if (json == null) {
                    throw new InvalidOperationException("No implementation of IJsonSerializer is registered.");
                }
                return json;
            }
        }

        public string MessageTableName { get; set; }

        public Task<long?> GetLastId() {
            return _store.GetLastId();
        }

        public Task Save(string key, object value) {
            // Save it locally then broadcast to other peers
            return GetMessageId(key)
                .ContinueWith(idTask => {
                    if (idTask.Exception != null) {
                        throw idTask.Exception;
                    }
                    var message = new Message(key, idTask.Result, value);
                    return Task.Factory.ContinueWhenAll(new[] {
                            _store.Save(message),
                            SendMessageToPeers(message)
                        },
                        _ => { }
                    );
                })
                .Unwrap();
        }

        public Task<IEnumerable<Message>> GetAll(string key) {
            return _store.GetAll(key);
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id) {
            return _store.GetAllSince(key, id);
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler) {
            _signalBus.AddHandler(eventKey, handler);
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler) {
            _signalBus.RemoveHandler(eventKey, handler);
        }

        public Task Signal(string eventKey) {
            // We only signal locally, peers were self-signaled when the message was sent
            return _signalBus.Signal(eventKey);
        }

        protected internal Task MessageReceived(string payload) {
            // Parse the payload into a message object, save to the store and signal the local bus
            var message = Json.Parse<WireMessage>(payload).ToMessage();
            return message != null
                ? _store.Save(message)
                    .ContinueWith(t => _signalBus.Signal(message.SignalKey))
                    .Unwrap()
                : TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Override this method to prepare the request before it is sent to peers, e.g. to add authentication credentials
        /// </summary>
        /// <param name="request">The request being sent to peers</param>
        protected virtual void PrepareRequest(WebRequest request) {
            
        }

        private Task<long> GetMessageId(string key) {
            var connection = CreateAndOpenConnection();
            var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
            var cmd = new SqlCommand(_getMessageIdSql.Replace("{TableName}", MessageTableName), connection, transaction);
            cmd.Parameters.AddWithValue("EventKey", key);
            return cmd.ExecuteScalarAsync<long>()
                .ContinueWith(idTask => {
                    if (idTask.Exception != null) {
                        throw idTask.Exception;
                    }
                    connection.Close();
                    //transaction.Rollback();
                    return idTask.Result;
                });
        }

        private Task SendMessageToPeers(Message message) {
            EnsurePeersDiscovered();
            // Loop through peers and send the message
            var queryString = "?" + SignalReceiverHandler.QueryStringKeys.EventKey + "=" + HttpUtility.UrlEncode(message.SignalKey);
            return Task.Factory.StartNew(() =>
                Parallel.ForEach(_peers, (peer) => {
                    var data = new Dictionary<string, string> {
                        { MessageReceiverHandler.Keys.Message, Json.Stringify(message) }
                    };
                    HttpHelper.PostAsync(peer + MessageReceiverHandler.HandlerName + queryString, PrepareRequest, data)
                        .ContinueWith(requestTask => {
                            if (requestTask.Exception == null) {
                                requestTask.Result.Close();
                            }
                        })
                        .Wait();
                })
            );
        }

        private void EnsurePeersDiscovered() {
            PeerToPeerHttpSignalBus.EnsurePeersDiscovered(ref _peersDiscovered, _peers, MessageReceiverHandler.HandlerName, Id, _peerDiscoveryLocker, PrepareRequest);
        }

        private SqlConnection CreateAndOpenConnection() {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }

    public class WireMessage {
        public string SignalKey { get; set; }
        public object Value { get; set; }
        public long Id { get; set; }
        public DateTime Created { get; set; }

        public Message ToMessage() {
            return new Message(SignalKey, Id, Value, Created);
        }
    }
}