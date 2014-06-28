using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisConnection : IRedisConnection
    {
        private string _key;
        private int _db;
        private ulong _latestValue;

        private StackExchange.Redis.ISubscriber _redisSubscriber;
        private ConnectionMultiplexer _connection;
        private Action<int, RedisMessage> _onMessage;

        public async Task ConnectAsync(string connectionString)
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ErrorMessage += OnError;
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParameter should not used", Justification = "This is to match external API")]
        public void Close(bool allowCommandsToComplete = true)
        {
            if (_redisSubscriber != null)
            {
                _redisSubscriber.Unsubscribe(_key);
            }

            if (_connection != null)
            {
                _connection.Close(allowCommandsToComplete);
            }

            _connection.Dispose();
        }

        public async Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage)
        {
            _key = key;
            _onMessage = onMessage;
            _redisSubscriber = _connection.GetSubscriber();
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

            _db = database;
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

        public async Task RestoreLatestValueForKey(TraceSource trace)
        {
            var redisResult = await _connection.GetDatabase(_db).ScriptEvaluateAsync(
               @"local newvalue = redis.call('GET', KEYS[1])
                    if newvalue < ARGV[1] then
                        return redis.call('SET',KEYS[1], ARGV[1])
                    else
                        return nil
                    end",
               new RedisKey[] { _key },
               new RedisValue[] { _latestValue });

            if (!redisResult.IsNull)
            {
                trace.TraceInformation("Restore Redis Key {0} to the latest Value {1} ", _key, _latestValue);
            }
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

            //save the _latestValue
            _latestValue = message.Id;
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

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "This is an exception for event.")]
        private void OnError(object sender, RedisErrorEventArgs args)
        {
            var handler = ErrorMessage;
            handler(new Exception(args.Message));
        }
    }
}
