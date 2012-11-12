// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private readonly int _db;
        private readonly string[] _keys;
        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private Task _connectTask;

        private readonly TaskQueue _publishQueue = new TaskQueue();

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

        protected override Task Send(Message[] messages)
        {
            return _connectTask.Then(msgs =>
            {
                var taskCompletionSource = new TaskCompletionSource<object>();

                // Group messages by source (connection id)
                var messagesBySource = msgs.GroupBy(m => m.Source);

                SendImpl(messagesBySource.GetEnumerator(), taskCompletionSource);

                return taskCompletionSource.Task;
            },
            messages);
        }

        private void SendImpl(IEnumerator<IGrouping<string, Message>> enumerator, TaskCompletionSource<object> taskCompletionSource)
        {
            if (!enumerator.MoveNext())
            {
                taskCompletionSource.TrySetResult(null);
            }
            else
            {
                IGrouping<string, Message> group = enumerator.Current;

                // Get the channel index we're going to use for this message
                int index = Math.Abs(group.Key.GetHashCode()) % _keys.Length;

                string key = _keys[index];

                // Increment the channel number
                _connection.Strings.Increment(_db, key)
                                   .Then((id, k) =>
                                   {
                                       var message = new RedisMessage(id, group.ToArray());

                                       return _connection.Publish(k, message.GetBytes());
                                   }, key)
                                   .Then((enumer, tcs) => SendImpl(enumer, tcs), enumerator, taskCompletionSource)
                                   .ContinueWithNotComplete(taskCompletionSource);
            }
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
            var message = RedisMessage.Deserialize(data);

            _publishQueue.Enqueue(() => OnReceived(key, (ulong)message.Id, message.Messages));
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
