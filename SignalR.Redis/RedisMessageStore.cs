using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BookSleeve;
using ProtoBuf;
using SignalR.Infrastructure;
using SignalR.ScaleOut;

namespace SignalR.Redis
{
    public class RedisMessageStore : IMessageStore
    {
        private const string MessageIdKey = "SignalR.MessageId";
        private const string MessagesKey = "SignalR.Messages";

        private RedisConnection _redisConnection;
        private int _database;
        private IJsonSerializer _jsonSerializer;

        public RedisMessageStore(RedisConnection redisConnection, int database)
        {
            _redisConnection = redisConnection;
            _database = database;
            _jsonSerializer = DependencyResolver.Resolve<IJsonSerializer>();
        }

        public Task<long?> GetLastId()
        {
            long id;

            var val = _redisConnection.Strings.GetString(_database, MessageIdKey, true).Result;
            if (Int64.TryParse(val, out id))
            {
                return Task.Factory.StartNew(() => (long?)id);
            }

            var tcs = new TaskCompletionSource<long?>();
            tcs.SetResult(id);
            return tcs.Task;
        }

        public Task Save(string key, object value)
        {
            var nextId = _redisConnection.Strings.Increment(_database, MessageIdKey, queueJump: true).Result;
            var message = new ProtoMessage
            {
                Created = DateTime.Now,
                SignalKey = key,
                Id = nextId,
                Value = _jsonSerializer.Stringify(value)
            };

            _redisConnection.SortedSets.Add(_database,
                                            MessagesKey,
                                            message.Serialize(),
                                            message.Id,
                                            true).Wait();

            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            var resultProtoMessages = _redisConnection.SortedSets.Range(_database,
                                                                   MessagesKey,
                                                                   (double)id,
                                                                   (double)long.MaxValue, 
                                                                   queueJump: true, 
                                                                   minInclusive: false)
                .Result
                .Select(o => ProtoMessage.Deserialize(o.Key));

            var resultMessages = resultProtoMessages
                .Select(o => new Message(o.SignalKey,
                                         o.Id,
                                         o.SignalKey.EndsWith(PersistentConnection.SignalrCommand) ? _jsonSerializer.Parse<SignalCommand>(o.Value) : _jsonSerializer.Parse(o.Value),
                                         o.Created));

            var tcs = new TaskCompletionSource<IEnumerable<Message>>();
            tcs.SetResult(resultMessages);
            return tcs.Task;
        }
    }

    [ProtoContract]
    public class ProtoMessage
    {
        public static ProtoMessage Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return Serializer.Deserialize<ProtoMessage>(ms);
            }
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        [ProtoMember(1)]
        public string SignalKey { get; set; }

        [ProtoMember(2)]
        public string Value { get; set; }

        [ProtoMember(3)]
        public long Id { get; set; }

        [ProtoMember(4)]
        public DateTime Created { get; set; }
    }
}