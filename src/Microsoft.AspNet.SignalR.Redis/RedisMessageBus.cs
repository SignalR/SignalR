// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using System.Text;
using System.Collections;

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

        //Marius Additions
        private readonly bool _publishOnly;
        private List<byte[]> messagesToIgnore = new List<byte[]>();
        private ulong lastUsedID = 0;

        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection connection)
            : this(resolver, configuration, connection, true)
        {
            messagesToIgnore.Add(Encoding.ASCII.GetBytes("{\"H\":\"ChatHub\",\"M\":\"echo\",\"A\":[]}"));
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

            _connectionString = configuration.ConnectionString;
            _db = configuration.Database;
            _key = configuration.EventKey;
            _publishOnly = configuration.PublishOnly;

            var traceManager = resolver.Resolve<ITraceManager>();

            _trace = traceManager["SignalR." + typeof(RedisMessageBus).Name];

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

        // For testing purposes only
        internal int ConnectionState { get { return _state; } }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {

            if (messages != null && messages.Count == 1 && !messages[0].IsCommand)
            {
                for (int i = 0; i < messagesToIgnore.Count; i++)
                {
                    bool equal = ((IStructuralEquatable)messagesToIgnore[i]).Equals(messages[0].Value.Array, StructuralComparisons.StructuralEqualityComparer);

                    //Don't propogate message to Redis
                    if (equal)
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            //Emit message back so we can act on it on this server
                            lock (_callbackLock)
                            {
                                //We need to increment this for each message or signalR won't act on it since it caches message IDs
                                lastUsedID = lastUsedID + 1;
                                OnReceived(streamIndex, lastUsedID, new ScaleoutMessage(messages));
                            }
                        });
                    }
                }
            }

            //If we already emmited message back, don't act on it
            return _connection.ScriptEvaluateAsync(_db,
        @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}",
        _key,
        RedisMessage.ToBytes(messages));

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
                _connection.Close(_key, allowCommandsToComplete: false);
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
            OnError(0, ex);
            _trace.TraceError("OnConnectionError - " + ex.Message);
        }

        private async void OnConnectionRestored(Exception ex)
        {
            await _connection.RestoreLatestValueForKey(_db, _key);

            _trace.TraceInformation("Connection restored");

            Interlocked.Exchange(ref _state, State.Connected);

            OpenStream(0);
        }

        internal async Task ConnectWithRetry()
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
        }

        private async Task ConnectToRedisAsync()
        {
            if (_connection != null)
            {
                _connection.ConnectionFailed -= OnConnectionFailed;
                _connection.ErrorMessage -= OnConnectionError;
                _connection.ConnectionRestored -= OnConnectionRestored;
            }

            _trace.TraceInformation("Connecting...");

            await _connection.ConnectAsync(_connectionString, _trace);

            _trace.TraceInformation("Connection opened");

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ErrorMessage += OnConnectionError;
            _connection.ConnectionRestored += OnConnectionRestored;

            //Don't subscribe if we are only a publisher
            if (!_publishOnly)
            {
                await _connection.SubscribeAsync(_key, OnMessage);
            }

            _trace.TraceVerbose("Subscribed to event " + _key);
        }

        private void OnMessage(int streamIndex, RedisMessage message)
        {
            // locked to avoid overlapping calls (even though we have set the mode 
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                lastUsedID = lastUsedID + 1;
                OnReceived(streamIndex, lastUsedID, message.ScaleoutMessage);
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

        // Internal for testing purposes
        internal static class State
        {
            public const int Closed = 0;
            public const int Connected = 1;
            public const int Disposing = 2;
            public const int Disposed = 3;
        }
    }
}
