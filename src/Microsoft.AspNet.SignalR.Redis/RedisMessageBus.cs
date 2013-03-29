// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private const int DefaultBufferSize = 1000;

        private readonly int _db;
        private readonly string _key;
        private readonly Func<RedisConnection> _connectionFactory;

        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private int _state;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed")]
        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connectionFactory = configuration.ConnectionFactory;
            _db = configuration.Database;
            _key = configuration.EventKey;

            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectWithRetry();
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var context = new SendContext(_key, messages, _connection);

            // Increment the channel number
            return _connection.Strings.Increment(_db, _key)
                               .Then((id, ctx) =>
                               {
                                   byte[] data = RedisMessage.ToBytes(id, ctx.Messages);

                                   return ctx.Connection.Publish(ctx.Key, data);
                               }, 
                               context);
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
                // Start buffering any sends that they are preserved
                OnError(0, e.Exception);

                // Retry until the connection reconnects
                ConnectWithRetry();
            }
        }

        private void OnMessage(string key, byte[] data)
        {
            // The key is the stream id (channel)
            var message = RedisMessage.FromBytes(data);

            OnReceived(0, (ulong)message.Id, message.Messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_channel != null)
                {
                    _channel.Unsubscribe(_key);
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

                    Open(0);
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
                    channel.Subscribe(_key, OnMessage);

                    // Dirty hack but it seems like subscribe returns before the actual
                    // subscription is properly setup in some cases
                    while (channel.SubscriptionCount == 0)
                    {
                        Thread.Sleep(500);
                    }

                    Trace.TraceEvent(TraceEventType.Verbose, 0, "Subscribed to events " + String.Join(",", _key));

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

        private class SendContext
        {
            public string Key;
            public IList<Message> Messages;
            public RedisConnection Connection;

            public SendContext(string key, IList<Message> messages, RedisConnection connection)
            {
                Key = key;
                Messages = messages;
                Connection = connection;
            }
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
