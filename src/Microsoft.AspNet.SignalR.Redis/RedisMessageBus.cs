// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Redis
{
    /// <summary>
    /// Uses Redis pub-sub to scale-out SignalR applications in web farms.
    /// </summary>
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private const int DefaultBufferSize = 1000;

        private readonly int _db;
        private readonly string _key;
        private readonly TraceSource _trace;
        private readonly ITraceManager _traceManager;

        private IRedisConnection _connection;
        private string _connectionString;
        private readonly object _callbackLock = new object();
        private ulong? lastId = null;

        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection connection)
            : this(resolver, configuration, connection, true)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ignore")]
        internal RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection connection, bool connectAutomatically)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connection = connection;
            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ErrorMessage += OnConnectionError;

            _connectionString = configuration.ConnectionString;
            _db = configuration.Database;
            _key = configuration.EventKey;

            _traceManager = resolver.Resolve<ITraceManager>();

            _trace = _traceManager["SignalR." + nameof(RedisMessageBus)];

            ReconnectDelay = TimeSpan.FromSeconds(2);

            if (connectAutomatically)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var ignore = ConnectWithRetry();
                });
            }
        }

        public TimeSpan ReconnectDelay { get; set; }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _connection.ScriptEvaluateAsync(
                _db,
                @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}",
                _key,
                RedisMessage.ToBytes(messages));
        }

        protected override void Dispose(bool disposing)
        {
            _trace.TraceInformation(nameof(RedisMessageBus) + " is being disposed");
            if (disposing)
            {
                Shutdown();
            }

            base.Dispose(disposing);
        }

        private void Shutdown()
        {
            _trace.TraceInformation("Shutdown()");

            if (_connection != null)
            {
                _connection.Close(_key, allowCommandsToComplete: false);
            }
        }

        private void OnConnectionFailed(Exception ex)
        {
            string errorMessage = (ex != null) ? ex.Message : Resources.Error_RedisConnectionClosed;

            _trace.TraceInformation("OnConnectionFailed - " + errorMessage);
        }

        private void OnConnectionError(Exception ex)
        {
            OnError(0, ex);
            _trace.TraceError("OnConnectionError - " + ex.Message);
        }

        private async void OnConnectionRestored(Exception ex)
        {
            await _connection.RestoreLatestValueForKey(_db, _key);

            _trace.TraceInformation("Connection restored");

            OpenStream(0);
        }

        internal async Task ConnectWithRetry()
        {
            while (true)
            {
                try
                {
                    await ConnectToRedisAsync();

                    _trace.TraceInformation("Opening stream.");
                    OpenStream(0);

                    break;
                }
                catch (Exception ex)
                {
                    _trace.TraceError("Error connecting to Redis - " + ex.GetBaseException());
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        private async Task ConnectToRedisAsync()
        {
            _trace.TraceInformation("Connecting...");

            // We need to hold the dispose lock during this in order to ensure that ConnectAsync completes fully without Dispose getting in the way
            await _connection.ConnectAsync(_connectionString, _traceManager["SignalR." + nameof(RedisConnection)]);

            _trace.TraceInformation("Connection opened");

            await _connection.SubscribeAsync(_key, OnMessage);

            _trace.TraceVerbose("Subscribed to event " + _key);
        }

        private void OnMessage(int streamIndex, RedisMessage message)
        {
            // locked to avoid overlapping calls (even though we have set the mode
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                if (lastId.HasValue && message.Id < lastId.Value)
                {
                    _trace.TraceEvent(TraceEventType.Error, 0, $"ID regression occurred. The next message ID {message.Id} was less than the previous message {lastId.Value}");
                }
                lastId = message.Id;
                OnReceived(streamIndex, message.Id, message.ScaleoutMessage);
            }
        }
    }
}
