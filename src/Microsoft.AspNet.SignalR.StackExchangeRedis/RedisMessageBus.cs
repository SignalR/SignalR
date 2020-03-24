// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.StackExchangeRedis
{
    /// <summary>
    /// Uses Redis pub-sub to scale-out SignalR applications in web farms.
    /// </summary>
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private const int DefaultBufferSize = 1000;

        private readonly RedisScaleoutEndpoint[] _endpoints;
        private readonly TraceSource _trace;
        private readonly ITraceManager _traceManager;

        private readonly IRedisConnection[] _connections;
        private readonly object _callbackLock = new object();
        private readonly ulong?[] _lastIds = null;

        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
            : this(resolver, configuration, true)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ignore")]
        internal RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration, bool connectAutomatically)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _traceManager = resolver.Resolve<ITraceManager>();

            _trace = _traceManager["SignalR." + nameof(RedisMessageBus)];

            ReconnectDelay = TimeSpan.FromSeconds(2);

            _endpoints = configuration.Endpoints;
            _connections = new IRedisConnection[_endpoints.Length];
            _lastIds = new ulong?[_endpoints.Length];

            for (int streamIndex = 0; streamIndex < _endpoints.Length; streamIndex++)
            {
                var streamIndexCopy = streamIndex;

                _connections[streamIndex] = new RedisConnection();
                _connections[streamIndex].ConnectionFailed += ex => OnConnectionFailed(streamIndexCopy, ex);
                _connections[streamIndex].ConnectionRestored += ex => OnConnectionRestored(streamIndexCopy, ex);
                _connections[streamIndex].ErrorMessage += ex => OnConnectionError(streamIndexCopy, ex);

                if (connectAutomatically)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var ignore = ConnectWithRetry(streamIndexCopy);
                    });
                }
            }
        }

        protected override int StreamCount
        {
            get
            {
                return _endpoints.Length;
            }
        }

        public TimeSpan ReconnectDelay { get; set; }

        public virtual void OpenStream(int streamIndex)
        {
            Open(streamIndex);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _connections[streamIndex].ScriptEvaluateAsync(
                _endpoints[streamIndex].Database,
                @"local newId = redis.call('INCR', KEYS[1])
                  local payload = newId .. ' ' .. ARGV[1]
                  redis.call('PUBLISH', KEYS[1], payload)
                  return {newId, ARGV[1], payload}",
                _endpoints[streamIndex].EventKey,
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

            if (_connections != null)
            {
                for (int i = 0; i < _endpoints.Length; i++)
                {
                    _connections[i].Close(_endpoints[i].EventKey, allowCommandsToComplete: false);
                }
            }
        }

        private void OnConnectionFailed(int streamIndex, Exception ex)
        {
            string errorMessage = (ex != null) ? ex.Message : Resources.Error_RedisConnectionClosed;

            _trace.TraceInformation($"OnConnectionFailed({streamIndex}, '{errorMessage}')");
        }

        private void OnConnectionError(int streamIndex, Exception ex)
        {
            OnError(streamIndex, ex);
            _trace.TraceError($"OnConnectionError({streamIndex}, '{ex.Message}')");
        }

        private async void OnConnectionRestored(int streamIndex, Exception ex)
        {
            await _connections[streamIndex].RestoreLatestValueForKey(_endpoints[streamIndex].Database, _endpoints[streamIndex].EventKey);

            _trace.TraceInformation($"Connection restored({streamIndex})");

            OpenStream(streamIndex);
        }

        internal async Task ConnectWithRetry(int streamIndex)
        {
            while (true)
            {
                try
                {
                    await ConnectToRedisAsync(streamIndex);

                    _trace.TraceInformation($"Opening stream {streamIndex}.");
                    OpenStream(streamIndex);

                    break;
                }
                catch (Exception ex)
                {
                    _trace.TraceError($"Error connecting to Redis endpoint '{_endpoints[streamIndex].ConnectionString}' - '{ex.GetBaseException()}'");
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        private async Task ConnectToRedisAsync(int streamIndex)
        {
            _trace.TraceInformation($"Connecting stream {streamIndex} to '{_endpoints[streamIndex].ConnectionString}' ...");

            // We need to hold the dispose lock during this in order to ensure that ConnectAsync completes fully without Dispose getting in the way
            await _connections[streamIndex].ConnectAsync(_endpoints[streamIndex].ConnectionString, _traceManager["SignalR." + nameof(RedisConnection)]);

            _trace.TraceInformation($"Stream'{streamIndex}' opened");

            await _connections[streamIndex].SubscribeAsync(_endpoints[streamIndex].EventKey, message => OnMessage(streamIndex, message));

            _trace.TraceVerbose($"Stream {streamIndex} subscribed to event '{_endpoints[streamIndex].EventKey}'");
        }

        private void OnMessage(int streamIndex, RedisMessage message)
        {
            // locked to avoid overlapping calls (even though we have set the mode
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                if (_lastIds[streamIndex].HasValue && message.Id < _lastIds[streamIndex].Value)
                {
                    _trace.TraceEvent(TraceEventType.Error, 0, $"ID regression occurred. The next message ID {message.Id} was less than the previous message {_lastIds[streamIndex].Value}");
                }
                _lastIds[streamIndex] = message.Id;
                OnReceived(streamIndex, message.Id, message.ScaleoutMessage);
            }
        }
    }
}
