// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private readonly int _db;
        private readonly string[] _keys;
        private readonly Func<RedisConnection> _connectionFactory;

        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private int _state;

        public RedisMessageBus(string server, int port, string password, int db, IList<string> keys, IDependencyResolver resolver)
            : this(GetConnectionFactory(server, port, password), db, keys, resolver)
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed")]
        public RedisMessageBus(Func<RedisConnection> connectionFactory, int db, IList<string> keys, IDependencyResolver resolver)
            : base(resolver)
        {
            _connectionFactory = connectionFactory;
            _db = db;
            _keys = keys.ToArray();

            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectWithRetry();
        }

        protected override int StreamCount
        {
            get
            {
                return _keys.Length;
            }
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            string key = _keys[streamIndex];

            // Increment the channel number
            return _connection.Strings.Increment(_db, key)
                               .Then((id, k) =>
                               {
                                   byte[] data = RedisMessage.ToBytes(id, messages);

                                   return _connection.Publish(k, data);
                               }, key);
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _state, State.Disposed);

            Trace.TraceInformation("OnConnectionClosed()");
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            Trace.TraceEvent(TraceEventType.Error, 0, "OnConnectionError - " + e.Cause + ". " + e.Exception.GetBaseException());

            // Change the state to closed and retry connecting
            if (Interlocked.CompareExchange(ref _state,
                                            State.Closed,
                                            State.Connected) == State.Connected)
            {
                // Tell the base class the connection died so it'll queue failed sends.
                Disconnect();

                // Retry until the connection reconnects
                ConnectWithRetry();
            }
        }

        private void OnMessage(string key, byte[] data)
        {
            // The key is the stream id (channel)
            var message = RedisMessage.FromBytes(data);

            OnReceived(key, (ulong)message.Id, message.Messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_channel != null)
                {
                    _channel.Unsubscribe(_keys);
                    _channel.Close(abort: true);
                }

                if (_connection != null)
                {
                    _connection.Close(abort: true);
                }
            }

            base.Dispose(disposing);
        }

        private void ConnectWithRetry()
        {
            // Attempt to change to connecting
            if (Interlocked.CompareExchange(ref _state,
                                            State.Connecting,
                                            State.Connected) == State.Connecting)
            {
                // Already connected so bail
                return;
            }

            Task connectTask = ConnectToRedis();

            connectTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    TaskAsyncHelper.Delay(ReconnectDelay)
                                   .Then(bus => bus.ConnectWithRetry(), this);
                }
                else
                {
                    Interlocked.Exchange(ref _state, State.Connected);

                    Connect();
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are caught")]
        private Task ConnectToRedis()
        {
            if (_connection != null)
            {
                _connection.Closed -= OnConnectionClosed;
                _connection.Error -= OnConnectionError;
                _connection.Dispose();
                _connection = null;
            }

            // Create a new connection to redis with the factory
            RedisConnection connection = _connectionFactory();

            connection.Closed += OnConnectionClosed;
            connection.Error += OnConnectionError;

            try
            {
                Trace.TraceInformation("Connecting...");

                // Start the connection
                return connection.Open().Then(() =>
                {
                    Trace.TraceInformation("Connection opened");

                    // Create a subscription channel in redis
                    RedisSubscriberConnection channel = connection.GetOpenSubscriberChannel();

                    // Subscribe to the registered connections
                    channel.Subscribe(_keys, OnMessage);

                    // Dirty hack but it seems like subscribe returns before the actual
                    // subscription is properly setup in some cases
                    while (channel.SubscriptionCount == 0)
                    {
                        Thread.Sleep(500);
                    }

                    Trace.TraceEvent(TraceEventType.Verbose, 0, "Subscribed to events " + String.Join(",", _keys));

                    _channel = channel;
                    _connection = connection;
                });
            }
            catch (Exception ex)
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Error connecting to redis - " + ex.GetBaseException());

                return TaskAsyncHelper.FromError(ex);
            }
        }

        private static Func<RedisConnection> GetConnectionFactory(string server, int port, string password)
        {
            return () => new RedisConnection(server, port: port, password: password);
        }

        private static class State
        {
            public const int Closed = 0;
            public const int Connecting = 1;
            public const int Connected = 2;
            public const int Disposed = 3;
        }
    }
}
