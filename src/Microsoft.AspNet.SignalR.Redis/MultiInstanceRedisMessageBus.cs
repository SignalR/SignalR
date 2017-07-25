﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Redis
{
    /// <summary>
    /// Uses Redis pub-sub instances to scale-out SignalR applications in web farms.
    /// </summary>
    public class MultiInstanceRedisMessageBus : ScaleoutMessageBus
    {
        private const int DefaultBufferSize = 1000;

        private readonly int _db;
        private readonly string _key;
        private readonly TraceSource _trace;
        private readonly ITraceManager _traceManager;

        private IRedisConnection[] _connections;
        private string[] _connectionStrings;
        private int _state;
        private readonly object _callbackLock = new object();
        private readonly SemaphoreSlim _redisConnectionEventLock = new SemaphoreSlim(1, 1);

        private readonly Random _rnd = new Random();

        public MultiInstanceRedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection[] connections)
            : this(resolver, configuration, connections, true)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ignore")]
        internal MultiInstanceRedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, IRedisConnection[] connections, bool connectAutomatically)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connections = connections;

            _connectionStrings = configuration.ConnectionStrings.ToArray();
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

        // For testing purposes only
        internal int ConnectionState { get { return _state; } }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return Task.Run(() =>
            {
                var commands = messages.Where(m => m.IsCommand).ToList();
                var publicatedMessages = messages.Where(m => !m.IsCommand).ToList();

                if (commands.Any()) // if there are commands -> distribute them to all connected instances
                {
                    Task[] tasks = new Task[_connections.Length];
                    for (int i = 0; i < _connections.Length; i++)
                    {
                        tasks[i] = _connections[i].ScriptEvaluateAsync(
                            _db,
                            @"local newId = redis.call('INCR', KEYS[1])
                            local payload = newId .. ' ' .. ARGV[1]
                            redis.call('PUBLISH', KEYS[1], payload)
                            return {newId, ARGV[1], payload}",
                            _key,
                            RedisMessage.ToBytes(commands));
                    }
                    Task.WaitAll(tasks);
                }

                if (publicatedMessages.Any()) // if there are published messages -> send them to the randon instance
                {
                    GetRandomInstanceConnection().ScriptEvaluateAsync(
                                _db,
                                @"local newId = redis.call('INCR', KEYS[1])
                            local payload = newId .. ' ' .. ARGV[1]
                            redis.call('PUBLISH', KEYS[1], payload)
                            return {newId, ARGV[1], payload}",
                                _key,
                                RedisMessage.ToBytes(publicatedMessages)).Wait();
                }
            });




        }

        protected override void Dispose(bool disposing)
        {
            _trace.TraceInformation(nameof(RedisMessageBus) + " is being disposed");
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

                _redisConnectionEventLock.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Shutdown()
        {
            _trace.TraceInformation("Shutdown()");

            if (_connections != null)
            {
                foreach (var connection in _connections)
                {
                    connection.Close(_key, allowCommandsToComplete: false);
                }
            }

            Interlocked.Exchange(ref _state, State.Disposed);
        }

        private void OnConnectionFailed(Exception ex)
        {
            // StackExchange Redis will raise this event twice when a connection fails -
            // once for ConnectionType.Interactive and once for ConnectionType.Subscription.
            // We could try being more granular but ignoring the subsequent event should suffice.
            try
            {
                _redisConnectionEventLock.Wait();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            try
            {
                if (_state == State.Closed)
                {
                    _trace.TraceVerbose("Duplicate ConnectionFailed event - ignoring");
                    return;
                }

                string errorMessage = (ex != null) ? ex.Message : Resources.Error_RedisConnectionClosed;

                _trace.TraceInformation("OnConnectionFailed - " + errorMessage);

                Interlocked.Exchange(ref _state, State.Closed);
            }
            finally
            {
                _redisConnectionEventLock.Release();
            }
        }

        private void OnConnectionError(Exception ex)
        {
            OnError(0, ex);
            _trace.TraceError("OnConnectionError - " + ex.Message);
        }

        private async void OnConnectionRestored(Exception ex)
        {
            // StackExchange Redis will raise this event twice when a connection is restored -
            // once for ConnectionType.Interactive and once for ConnectionType.Subscription.
            // We could try being more granular but ignoring the subsequent event should suffice.
            try
            {
                _redisConnectionEventLock.Wait();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            try
            {
                if (_state == State.Connected)
                {
                    _trace.TraceVerbose("Duplicate ConnectionRestored event - ignoring");
                    return;
                }

                foreach (var connection in _connections)
                    await connection.RestoreLatestValueForKey(_db, _key);

                _trace.TraceInformation("Connection restored");

                Interlocked.Exchange(ref _state, State.Connected);

                OpenStream(0);
            }
            finally
            {
                _redisConnectionEventLock.Release();
            }
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
                        _trace.TraceInformation("Opening stream.");
                        OpenStream(0);
                    }
                    else
                    {
                        Debug.Assert(oldState == State.Disposing, "unexpected state");
                        _trace.TraceError("Unexpected state.");

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
                    _trace.TraceInformation("MessageBus is disposing.");
                    Shutdown();
                    break;
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        private async Task ConnectToRedisAsync()
        {
            if (_connections != null)
            {
                foreach (var connection in _connections)
                {
                    connection.ConnectionFailed -= OnConnectionFailed;
                    connection.ErrorMessage -= OnConnectionError;
                    connection.ConnectionRestored -= OnConnectionRestored;
                }
            }

            _trace.TraceInformation("Connecting...");

            for (int i = 0; i < _connections.Length; i++)
            {
                await _connections[i].ConnectAsync(_connectionStrings[i], _traceManager["SignalR." + nameof(RedisConnection)]);
            }

            _trace.TraceInformation("Connection opened");
            foreach (var connection in _connections)
            {
                connection.ConnectionFailed += OnConnectionFailed;
                connection.ErrorMessage += OnConnectionError;
                connection.ConnectionRestored += OnConnectionRestored;
                await connection.SubscribeAsync(_key, OnMessage);
            }

            _trace.TraceVerbose("Subscribed to event " + _key);
        }

        private IRedisConnection GetRandomInstanceConnection()
        {
            return _connections[_rnd.Next(0, _connections.Length)];
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
