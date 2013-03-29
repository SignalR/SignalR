// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.ServiceBus.Infrastructure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusConnection : IDisposable
    {
        private const int ReceiveBatchSize = 1000;
        private static readonly TimeSpan BackoffAmount = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan MessageTtl = TimeSpan.FromMinutes(1);

        private readonly NamespaceManager _namespaceManager;
        private readonly MessagingFactory _factory;
        private readonly string _connectionString;

        private readonly ConcurrentDictionary<string, TopicClient> _clients = new ConcurrentDictionary<string, TopicClient>();

        public ServiceBusConnection(string connectionString)
        {
            _connectionString = connectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _factory = MessagingFactory.CreateFromConnectionString(connectionString);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable is returned to the caller")]
        public IDisposable Subscribe(IList<string> topicNames, Action<int, IEnumerable<BrokeredMessage>> handler, Action<int> open)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException("topicNames");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (open == null)
            {
                throw new ArgumentNullException("open");
            }

            var subscriptions = new List<Subscription>();

            for (var topicIndex = 0; topicIndex < topicNames.Count; ++topicIndex)
            {
                string topicName = topicNames[topicIndex];

                if (!_namespaceManager.TopicExists(topicName))
                {
                    _namespaceManager.CreateTopic(topicName);
                }

                // Create a client for this topic
                _clients.TryAdd(topicName, TopicClient.CreateFromConnectionString(_connectionString, topicName));

                // Create a random subscription
                string subscriptionName = Guid.NewGuid().ToString();
                _namespaceManager.CreateSubscription(topicName, subscriptionName);

                // Create a receiver to get messages
                string subscriptionEntityPath = SubscriptionClient.FormatSubscriptionPath(topicName, subscriptionName);
                MessageReceiver receiver = _factory.CreateMessageReceiver(subscriptionEntityPath);

                subscriptions.Add(new Subscription(topicName, subscriptionName, receiver));

                open(topicIndex);

                PumpMessages(topicIndex, receiver, handler);
            }

            return new DisposableAction(() =>
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Receiver.Close();

                    _namespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);

                    TopicClient client;
                    if (_clients.TryRemove(subscription.TopicPath, out client))
                    {
                        client.Close();
                    }
                }
            });
        }

        public Task Publish(string topicName, Stream stream)
        {
            TopicClient client;
            if (_clients.TryGetValue(topicName, out client))
            {
                var message = new BrokeredMessage(stream, ownsStream: true)
                {
                    TimeToLive = MessageTtl
                };

                return client.SendAsync(message);
            }

            // REVIEW: Should this return a faulted task?
            return TaskAsyncHelper.Empty;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Close the factory
                _factory.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void PumpMessages(int topicIndex, MessageReceiver receiver, Action<int, IEnumerable<BrokeredMessage>> handler)
        {
        receive:

            IAsyncResult result = null;

            try
            {
                result = receiver.BeginReceiveBatch(ReceiveBatchSize, ar =>
                {
                    if (ar.CompletedSynchronously)
                    {
                        return;
                    }

                    var state = (ReceiveState)ar.AsyncState;

                    if (ContinueReceiving(ar, state.TopicIndex, state.Receiver, state.Handler))
                    {
                        PumpMessages(state.TopicIndex, state.Receiver, state.Handler);
                    }
                },
                new ReceiveState(topicIndex, receiver, handler));
            }
            catch (OperationCanceledException)
            {
                // This means the channel is closed
                return;
            }

            if (result.CompletedSynchronously)
            {
                if (ContinueReceiving(result, topicIndex, receiver, handler))
                {
                    goto receive;
                }
            }
        }

        private bool ContinueReceiving(IAsyncResult asyncResult, int topicIndex, MessageReceiver receiver, Action<int, IEnumerable<BrokeredMessage>> handler)
        {
            bool backOff = false;

            try
            {
                handler(topicIndex, receiver.EndReceiveBatch(asyncResult));
            }
            catch (ServerBusyException)
            {
                // Too busy so back off
                backOff = true;
            }
            catch (OperationCanceledException)
            {
                // This means the channel is closed
                return false;
            }

            if (backOff)
            {
                TaskAsyncHelper.Delay(BackoffAmount)
                               .Then(() => PumpMessages(topicIndex, receiver, handler));
            }

            // true -> continue reading normally
            // false -> Don't continue reading as we're backing off
            return !backOff;
        }

        private class Subscription
        {
            public Subscription(string topicPath, string subName, MessageReceiver receiver)
            {
                TopicPath = topicPath;
                Name = subName;
                Receiver = receiver;
            }

            public string TopicPath { get; private set; }
            public string Name { get; private set; }
            public MessageReceiver Receiver { get; private set; }
        }

        private class ReceiveState
        {
            public ReceiveState(int topicIndex, MessageReceiver receiver, Action<int, IEnumerable<BrokeredMessage>> handler)
            {
                TopicIndex = topicIndex;
                Receiver = receiver;
                Handler = handler;
            }

            public int TopicIndex { get; private set; }
            public MessageReceiver Receiver { get; private set; }
            public Action<int, IEnumerable<BrokeredMessage>> Handler { get; private set; }
        }
    }
}
