﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisConnection : IRedisConnection
    {
        private StackExchange.Redis.ISubscriber _redisSubscriber;
        private ConnectionMultiplexer _connection;
        private TraceSource _trace;
        private ulong _latestMessageId;

        public async Task ConnectAsync(string connectionString, TraceSource trace)
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionString, new TraceTextWriter("ConnectionMultiplexer: ", trace));
            if (!_connection.IsConnected)
            {
                _connection.Dispose();
                _connection = null;
                throw new InvalidOperationException("Failed to connect to Redis");
            }

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ErrorMessage += OnError;

            _trace = trace;

            _redisSubscriber = _connection.GetSubscriber();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void Close(string key, bool allowCommandsToComplete = true)
        {
            _trace.TraceInformation("Closing key: " + key);
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
            _trace.TraceInformation("Subscribing to key: " + key);
            await _redisSubscriber.SubscribeAsync(key, (channel, data) =>
            {
                var message = RedisMessage.FromBytes(data, _trace);
                onMessage(0, message);

                // Save the last message id in just in case redis shuts down
                _latestMessageId = message.Id;
            });
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        public Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(Resources.Error_RedisConnectionNotStarted);
            }

            var keys = new RedisKey[] { key };

            var arguments = new RedisValue[] { messageArguments };

            return _connection.GetDatabase(database).ScriptEvaluateAsync(script,
                keys,
                arguments);
        }

        public async Task RestoreLatestValueForKey(int database, string key)
        {
            try
            {
                // Workaround for StackExchange.Redis/issues/61 that sometimes Redis connection is not connected in ConnectionRestored event
                while (!_connection.GetDatabase(database).IsConnected(key))
                {
                    await Task.Delay(200);
                }

                var redisResult = await _connection.GetDatabase(database).ScriptEvaluateAsync(
                   @"local newvalue = tonumber(redis.call('GET', KEYS[1]))
                     if not newvalue or tonumber(newvalue) < tonumber(ARGV[1]) then
                         return redis.call('SET', KEYS[1], ARGV[1])
                     else
                         return nil
                     end",
                   new RedisKey[] { key },
                   new RedisValue[] { _latestMessageId });

                if (!redisResult.IsNull)
                {
                    _trace.TraceInformation("Restored Redis Key {0} to the latest Value {1} ", key, _latestMessageId);
                }
            }
            catch (Exception ex)
            {
                _trace.TraceError("Error while restoring Redis Key to the latest Value: " + ex);
            }
        }

        public event Action<Exception> ConnectionFailed;

        public event Action<Exception> ConnectionRestored;

        public event Action<Exception> ErrorMessage;

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs args)
        {
            _trace.TraceWarning("Connection failed. Reason: " + args.FailureType.ToString() + " Exception: " + args.Exception.ToString());
            var handler = ConnectionFailed;
            handler(args.Exception);
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs args)
        {
            _trace.TraceInformation("Connection restored");
            var handler = ConnectionRestored;
            handler(args.Exception);
        }

        private void OnError(object sender, RedisErrorEventArgs args)
        {
            _trace.TraceWarning("Redis Error: " + args.Message);
            var handler = ErrorMessage;
            handler(new InvalidOperationException(args.Message));
        }
    }
}
