﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private IRedisConnection _connection;
        private string _connectionString;
        private int _state;
        private readonly object _callbackLock = new object();
        private readonly TaskCompletionSource<object> _tcs;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed")]
        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection connection)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connection = connection;

            _connectionString = configuration.ConnectionString;
            _db = configuration.Database;
            _key = configuration.EventKey;

            var traceManager = resolver.Resolve<ITraceManager>();

            _trace = traceManager["SignalR." + typeof(RedisMessageBus).Name];

            ReconnectDelay = TimeSpan.FromSeconds(2);

            _tcs = new TaskCompletionSource<object>();

            Task.Run(() => ConnectWithRetry());
        }

        public TimeSpan ReconnectDelay { get; set; }

        // For testing purposes only
        public int ConnectionState { get { return _state; } }

        // For testing purposes only
        public Task<object> InitialConnectTask { get { return _tcs.Task; } }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var redisTask = _connection.ScriptEvaluateAsync(_db,
                                @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}  
                ",
                _key,
                RedisMessage.ToBytes(messages));

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

            if (_connection != null)
            {
                _connection.Close(allowCommandsToComplete: false);
            }

            Interlocked.Exchange(ref _state, State.Disposed);
        }

        private void OnConnectionFailed(Exception ex)
        {
            string errorMessage = (ex != null) ? ex.Message : Resources.Error_RedisConnectionClosed;

            _trace.TraceInformation("OnConnectionFailed - " + errorMessage);

            Interlocked.Exchange(ref _state, State.Closed);
        }

        private void OnConnectionError(Exception ex)
        {
            _trace.TraceError("OnConnectionError - " + ex.Message);
        }

        private void OnConnectionRestored(Exception ex)
        {
            _trace.TraceInformation("OnConnectionRestored");

            Interlocked.Exchange(ref _state, State.Connected);

            OpenStream(0);
        }

        private async void ConnectWithRetry()
        {
            while (true)
            {
                try
                {
                    await ConnectToRedisAsync();

                    var oldState = Interlocked.CompareExchange(ref _state,
                                               State.Connected,
                                               State.Closed);

                    if (oldState == State.Closed)
                    {
                        OpenStream(0);
                    }
                    else
                    {
                        Debug.Assert(oldState == State.Disposing, "unexpected state");

                        Shutdown();
                    }

                    break;
                }

                catch (Exception ex)
                {
                    _trace.TraceError("Error connecting to Redis - " + ex.GetBaseException());
                }

                if (_state == State.Disposing)
                {
                    Shutdown();
                    break;
                }

                await Task.Delay(ReconnectDelay);
            }

            _tcs.TrySetResult(null);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are caught")]
        private async Task ConnectToRedisAsync()
        {
            if (_connection != null)
            {
                _connection.ConnectionFailed -= OnConnectionFailed;
                _connection.ErrorMessage -= OnConnectionError;
            }

            _trace.TraceInformation("Connecting...");

            await _connection.ConnectAsync(_connectionString);

            _trace.TraceInformation("Connection opened");

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ErrorMessage += OnConnectionError;
            _connection.ConnectionRestored += OnConnectionRestored;

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

        public static class State
        {
            public const int Closed = 0;
            public const int Connected = 1;
            public const int Disposing = 2;
            public const int Disposed = 3;
        }
    }
}
