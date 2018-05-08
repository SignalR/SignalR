// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using StackExchange.Redis;

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
            string GetMessageContent(Message message)
            {
                var content = Encoding.UTF8.GetString(message.Value.Array, message.Value.Offset, message.Value.Count);
                return $"Key:{message.Key},ID:{message.MappingId},Content:{content}";
            }

            async Task WaitForAndTraceResult(Task<RedisResult> task)
            {
                var result = await task;
                var values = (RedisValue[]) result;
                _trace.TraceEvent(TraceEventType.Verbose, 0, $"Published message with key: {(int)values[0]}");
            }

            if (_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                var messageContent = "[" + string.Join(";", messages.Select(m => GetMessageContent(m))) + "]";
                _trace.TraceEvent(TraceEventType.Verbose, 0, "Publishing message: {0}", messageContent);
            }

            var execTask = _connection.ScriptEvaluateAsync(
                _db,
                @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}",
                _key,
                RedisMessage.ToBytes(messages));

            // Sketchy AF, but we know that our RedisConnection returns the Task<RedisResult> as the Task, so we can downcast
            if (_trace.Switch.ShouldTrace(TraceEventType.Verbose) && execTask is Task<RedisResult> resultTask)
            {
                return WaitForAndTraceResult(resultTask);
            }

            return execTask;
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
                OnReceived(streamIndex, message.Id, message.ScaleoutMessage);
            }
        }
    }
}
