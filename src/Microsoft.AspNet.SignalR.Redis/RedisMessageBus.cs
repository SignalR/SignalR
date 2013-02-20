// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private readonly int _db;
        private readonly string[] _keys;
        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private Task _connectTask;

        public RedisMessageBus(string server, int port, string password, int db, IEnumerable<string> keys, IDependencyResolver resolver)
            : base(resolver)
        {
            _db = db;
            _keys = keys.ToArray();

            _connection = new RedisConnection(host: server, port: port, password: password);

            _connection.Closed += OnConnectionClosed;
            _connection.Error += OnConnectionError;

            // Start the connection
            _connectTask = _connection.Open().Then(() =>
            {
                // Create a subscription channel in redis
                _channel = _connection.GetOpenSubscriberChannel();

                // Subscribe to the registered connections
                _channel.Subscribe(_keys, OnMessage);

                // Dirty hack but it seems like subscribe returns before the actual
                // subscription is properly setup in some cases
                while (_channel.SubscriptionCount == 0)
                {
                    Thread.Sleep(500);
                }
            });
        }

        protected override int StreamCount
        {
            get
            {
                return _keys.Length;
            }
        }

        protected override Task Send(IList<Message> messages)
        {
            return _connectTask.Then(msgs => base.Send(msgs), messages);
        }

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
            // Should we auto reconnect?
        }

        private void OnConnectionError(object sender, BookSleeve.ErrorEventArgs e)
        {
            // How do we bubble errors?
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
    }
}
