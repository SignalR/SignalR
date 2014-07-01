using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisConnection : IRedisConnection
    {
        private TraceSource _trace;
        private StackExchange.Redis.ISubscriber _redisSubscriber;
        private ConnectionMultiplexer _connection;
        private Action<int, RedisMessage> _onMessage;

        public async Task ConnectAsync(string connectionString, TraceSource trace)
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ErrorMessage += OnError;

            _trace = trace;
            _redisSubscriber = _connection.GetSubscriber();
        }

        public void Close(string key, bool allowCommandsToComplete = true)
        {
            if (_redisSubscriber != null)
            {
                _redisSubscriber.Unsubscribe(key);
            }

            if (_connection != null)
            {
                _connection.Close(allowCommandsToComplete);
            }

            _connection.Dispose();
        }

        public async Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage)
        {
            _onMessage = onMessage;
            await _redisSubscriber.SubscribeAsync(key, OnMessage);
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        public async Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments)
        {
            if (_connection == null)
            {
                throw new Exception();
            }

            var keys = new RedisKey[] { key };

            var arguments = new RedisValue[] { messageArguments };

            await _connection.GetDatabase(database).ScriptEvaluateAsync(
                @"local newId = redis.call('INCR', KEYS[1])
                              local payload = newId .. ' ' .. ARGV[1]
                              redis.call('PUBLISH', KEYS[1], payload)
                              return {newId, ARGV[1], payload}  
                            ",
                keys,
                arguments);
        }

        public event Action<Exception> ConnectionFailed;

        public event Action<Exception> ConnectionRestored;

        public event Action<Exception> ErrorMessage;

        private void OnMessage(RedisChannel key, RedisValue data)
        {
            var trace = new TraceSource("Redis Connection");

            // The key is the stream id (channel)
            var message = RedisMessage.FromBytes(data, trace);
            _onMessage(0, message);
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs args)
        {
            var handler = ConnectionFailed;
            handler(args.Exception);
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs args)
        {
            var handler = ConnectionRestored;
            handler(args.Exception);
        }

        private void OnError(object sender, RedisErrorEventArgs args)
        {
            var handler = ErrorMessage;
            handler(new Exception(args.Message));
        }
    }
}
