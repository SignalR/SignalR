using System;
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

        public ServiceBusConnection(string connectionString)
        {
            _connectionString = connectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _factory = MessagingFactory.CreateFromConnectionString(connectionString);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable is returned to the caller")]
        public IDisposable Subscribe(IList<string> topicNames, Action<string, IEnumerable<BrokeredMessage>> handler)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException("topicNames");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            var subscriptions = new List<Subscription>();

            foreach (var topicPath in topicNames)
            {
                if (!_namespaceManager.TopicExists(topicPath))
                {
                    _namespaceManager.CreateTopic(topicPath);
                }

                // Create a random subscription
                string subscriptionName = Guid.NewGuid().ToString();
                _namespaceManager.CreateSubscription(topicPath, subscriptionName);

                // Create a receiver to get messages
                string subscriptionEntityPath = SubscriptionClient.FormatSubscriptionPath(topicPath, subscriptionName);
                MessageReceiver receiver = _factory.CreateMessageReceiver(subscriptionEntityPath);

                subscriptions.Add(new Subscription(topicPath, subscriptionName, receiver));

                PumpMessages(topicPath, receiver, handler);
            }

            return new DisposableAction(() =>
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Receiver.Close();

                    _namespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);
                }
            });
        }

        public Task Publish(string topicName, Stream stream)
        {
            // REVIEW: Do we need to keep track of these and clean this up on Dispose?
            var client = TopicClient.CreateFromConnectionString(_connectionString, topicName);
            var message = new BrokeredMessage(stream, ownsStream: true)
            {
                TimeToLive = MessageTtl
            };

            return client.SendAsync(message);
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

        private void PumpMessages(string topicPath, MessageReceiver receiver, Action<string, IEnumerable<BrokeredMessage>> handler)
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

                    if (ContinueReceiving(ar, state.TopicPath, state.Receiver, state.Handler))
                    {
                        PumpMessages(state.TopicPath, state.Receiver, state.Handler);
                    }
                },
                new ReceiveState(topicPath, receiver, handler));
            }
            catch (OperationCanceledException)
            {
                // This means the channel is closed
                return;
            }

            if (result.CompletedSynchronously)
            {
                if (ContinueReceiving(result, topicPath, receiver, handler))
                {
                    goto receive;
                }
            }
        }

        private bool ContinueReceiving(IAsyncResult asyncResult, string topicPath, MessageReceiver receiver, Action<string, IEnumerable<BrokeredMessage>> handler)
        {
            bool backOff = false;

            try
            {
                handler(topicPath, receiver.EndReceiveBatch(asyncResult));
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
                               .Then(() => PumpMessages(topicPath, receiver, handler));
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
            public ReceiveState(string topicPath, MessageReceiver receiver, Action<string, IEnumerable<BrokeredMessage>> handler)
            {
                TopicPath = topicPath;
                Receiver = receiver;
                Handler = handler;
            }

            public string TopicPath { get; private set; }
            public MessageReceiver Receiver { get; private set; }
            public Action<string, IEnumerable<BrokeredMessage>> Handler { get; private set; }
        }
    }
}
