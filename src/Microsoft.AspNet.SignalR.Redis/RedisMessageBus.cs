﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private ConnectionMultiplexer _connection;
        private string _connectionString;
        StackExchange.Redis.ISubscriber _subscriber;
        private int _state;
        private readonly object _callbackLock = new object();

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed")]
        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connectionString = configuration.ConnectionString;
            _db = configuration.Database;
            _key = configuration.EventKey;

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(RedisMessageBus).Name];

            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectWithRetry();
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var keys = new RedisKey[] { _key };

            //TraceMessages(messages);

            var arguments = new RedisValue[] { RedisMessage.ToBytes(messages) };

            var redisTask = _connection.GetDatabase(_db).ScriptEvaluateAsync(
                @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}
                ",
                keys,
                arguments);

            //Task.Run(() => TraceRedisScriptResult(redisTask));

            return redisTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var oldState = Interlocked.Exchange(ref _state, State.Disposing);

                switch (oldState)
                {
                    case State.Connected:
                        Shutdown();
                        break;
                    case State.Closed:
                    case State.Disposing:
                        // No-op
                        break;
                    case State.Disposed:
                        Interlocked.Exchange(ref _state, State.Disposed);
                        break;
                    default:
                        break;
                }
            }

            base.Dispose(disposing);
        }

        private void Shutdown()
        {
            _trace.TraceInformation("Shutdown()");

            if (_subscriber != null)
            {
                _subscriber.Unsubscribe(_key);
            }

            if (_connection != null)
            {
                _connection.Close(allowCommandsToComplete: false);
            }

            Interlocked.Exchange(ref _state, State.Disposed);
        }

        private void OnConnectionClosed(object sender, ConnectionFailedEventArgs e)
        {
            Exception ex = (e.Exception != null) ? e.Exception : new RedisConnectionClosedException();

            _trace.TraceInformation("OnConnectionClosed()");

            AttemptReconnect(ex);
        }

        private void OnConnectionError(object sender, RedisErrorEventArgs e)
        {
            _trace.TraceError("OnConnectionError - " + e.Message);

            AttemptReconnect(new RedisConnectionException(e.Message));
        }


        private void AttemptReconnect(Exception exception)
        {
            // Change the state to closed and retry connecting
            var oldState = Interlocked.CompareExchange(ref _state,
                                                       State.Closed,
                                                       State.Connected);
            if (oldState == State.Connected)
            {
                _trace.TraceInformation("Attempting reconnect...");

                // Let the base class know that an error occurred
                OnError(0, exception);

                // Retry until the connection reconnects
                ConnectWithRetry();
            }
        }

        private void OnMessage(RedisChannel key, RedisValue data)
        {
            // The key is the stream id (channel)
            var message = RedisMessage.FromBytes(data, _trace);

            // locked to avoid overlapping calls (even though we have set the mode 
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                OnReceived(0, message.Id, message.ScaleoutMessage);
            }
        }

        private void ConnectWithRetry()
        {
            Task connectTask = ConnectToRedis();

            connectTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _trace.TraceError("Error connecting to Redis - " + task.Exception.GetBaseException());

                    if (_state == State.Disposing)
                    {
                        Shutdown();
                        return;
                    }

                    TaskAsyncHelper.Delay(ReconnectDelay)
                                   .Then(bus => bus.ConnectWithRetry(), this);
                }
                else
                {
                    var oldState = Interlocked.CompareExchange(ref _state,
                                                               State.Connected,
                                                               State.Closed);
                    if (oldState == State.Closed)
                    {
                        Open(0);
                    }
                    else if (oldState == State.Disposing)
                    {
                        Shutdown();
                    }
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are caught")]
        private Task ConnectToRedis()
        {
            if (_connection != null)
            {
                _connection.ConnectionFailed -= OnConnectionClosed;
                _connection.ErrorMessage -= OnConnectionError;
                _connection.Dispose();
                _connection = null;
            }


            try
            {
                _trace.TraceInformation("Connecting...");

                return ConnectionMultiplexer.ConnectAsync(_connectionString).Then((conn) =>
                {
                    _connection = conn;
                    _trace.TraceInformation("Connection opened");

                    _connection.ConnectionFailed += OnConnectionClosed;
                    _connection.ErrorMessage += OnConnectionError;

                    _subscriber = _connection.GetSubscriber();

                    _subscriber.SubscribeAsync(_key, OnMessage).Then(() =>
                    {
                        _trace.TraceVerbose("Subscribed to event " + _key);
                    });

                });
            }
            catch (Exception ex)
            {
                _trace.TraceError("Error connecting to Redis - " + ex.GetBaseException());

                return TaskAsyncHelper.FromError(ex);
            }
        }

        private void TraceMessages(IList<Message> messages)
        {
            if (!_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                return;
            }

            foreach (Message message in messages)
            {
                _trace.TraceVerbose("Sending {0} bytes over Redis Bus: {1}", message.Value.Array.Length, message.GetString());
            }
        }

        private void TraceRedisScriptResult(Task<object> redisTask)
        {
            if (!_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                return;
            }

            var result = redisTask.Result as object[];
            var argumentNames = new string[] { "newId", "message", "payload" };

            for (var i = 0; i < result.Length; i++)
            {
                var r = result[i];
                _trace.TraceVerbose("Sending {0}: ({1}) {2}", argumentNames[i], r.GetType().Name, FormatBytes(r));
            }
        }

        private static string FormatBytes(object payload)
        {
            byte[] bytes = payload as byte[];
            if (bytes != null)
            {
                return bytes.Length + " bytes: " + BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
            return payload.ToString();
        }

        private static class State
        {
            public const int Closed = 0;
            public const int Connected = 1;
            public const int Disposing = 2;
            public const int Disposed = 3;
        }
    }
}
