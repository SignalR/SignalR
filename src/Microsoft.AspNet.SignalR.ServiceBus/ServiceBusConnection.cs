// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    internal class ServiceBusConnection : IDisposable
    {
        private const int DefaultReceiveBatchSize = 1000;
        private static readonly TimeSpan BackoffAmount = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ErrorBackOffAmount = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ErrorReadTimeout = TimeSpan.FromSeconds(0.5);

        private readonly NamespaceManager _namespaceManager;
        private readonly MessagingFactory _factory;
        private readonly ServiceBusScaleoutConfiguration _configuration;

        public ServiceBusConnection(ServiceBusScaleoutConfiguration configuration)
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(configuration.ConnectionString);
            _factory = MessagingFactory.CreateFromConnectionString(configuration.ConnectionString);
            _configuration = configuration;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable is returned to the caller")]
        public ServiceBusSubscription Subscribe(IList<string> topicNames,
                                                Action<int, IEnumerable<BrokeredMessage>> handler,
                                                Action<int, Exception> errorHandler)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException("topicNames");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            var subscriptions = new ServiceBusSubscription.SubscriptionContext[topicNames.Count];
            var clients = new TopicClient[topicNames.Count];

            for (var topicIndex = 0; topicIndex < topicNames.Count; ++topicIndex)
            {
                string topicName = topicNames[topicIndex];

                if (!_namespaceManager.TopicExists(topicName))
                {
                    try
                    {
                        _namespaceManager.CreateTopic(topicName);
                    }
                    catch (MessagingEntityAlreadyExistsException)
                    {
                        // The entity already exists
                    }
                }

                // Create a client for this topic
                clients[topicIndex] = TopicClient.CreateFromConnectionString(_configuration.ConnectionString, topicName);

                // Create a random subscription
                string subscriptionName = Guid.NewGuid().ToString();

                try
                {
                    _namespaceManager.CreateSubscription(topicName, subscriptionName);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // The entity already exists
                }

                // Create a receiver to get messages
                string subscriptionEntityPath = SubscriptionClient.FormatSubscriptionPath(topicName, subscriptionName);
                MessageReceiver receiver = _factory.CreateMessageReceiver(subscriptionEntityPath, ReceiveMode.ReceiveAndDelete);

                subscriptions[topicIndex] = new ServiceBusSubscription.SubscriptionContext(topicName, subscriptionName, receiver);

                var receiverContext = new ReceiverContext(topicIndex, receiver, handler, errorHandler);

                ProcessMessages(receiverContext);
            }

            return new ServiceBusSubscription(_configuration, _namespaceManager, subscriptions, clients);
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are handled through the error handler callback")]
        private void ProcessMessages(ReceiverContext receiverContext)
        {
        receive:

            try
            {
                IAsyncResult result = receiverContext.Receiver.BeginReceiveBatch(receiverContext.ReceiveBatchSize, receiverContext.ReceiveTimeout, ar =>
                {
                    if (ar.CompletedSynchronously)
                    {
                        return;
                    }

                    var ctx = (ReceiverContext)ar.AsyncState;

                    if (ContinueReceiving(ar, ctx))
                    {
                        ProcessMessages(ctx);
                    }
                },
                receiverContext);

                if (result.CompletedSynchronously)
                {
                    if (ContinueReceiving(result, receiverContext))
                    {
                        goto receive;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This means the channel is closed
                return;
            }
            catch (Exception ex)
            {
                receiverContext.OnError(ex);

                // REVIEW: What should we do here?
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are handled through the error handler callback")]
        private bool ContinueReceiving(IAsyncResult asyncResult, ReceiverContext receiverContext)
        {
            bool shouldContinue = true;
            TimeSpan backoffAmount = BackoffAmount;

            try
            {
                IEnumerable<BrokeredMessage> messages = receiverContext.Receiver.EndReceiveBatch(asyncResult);

                receiverContext.OnMessage(messages);

                // Reset the receive timeout if it changed
                receiverContext.ReceiveTimeout = DefaultReadTimeout;
            }
            catch (ServerBusyException ex)
            {
                receiverContext.OnError(ex);

                // Too busy so back off
                shouldContinue = false;
            }
            catch (OperationCanceledException)
            {
                // This means the channel is closed
                return false;
            }
            catch (Exception ex)
            {
                receiverContext.OnError(ex);

                shouldContinue = false;

                // TODO: Exponential backoff
                backoffAmount = ErrorBackOffAmount;

                // After an error, we want to adjust the timeout so that we
                // can recover as quickly as possible even if there's no message
                receiverContext.ReceiveTimeout = ErrorReadTimeout;
            }

            if (!shouldContinue)
            {
                TaskAsyncHelper.Delay(backoffAmount)
                               .Then(ctx => ProcessMessages(ctx), receiverContext);

                return false;
            }

            return true;
        }

        private class ReceiverContext
        {
            private readonly int _topicIndex;
            private readonly Action<int, IEnumerable<BrokeredMessage>> _handler;
            private readonly Action<int, Exception> _errorHandler;

            public readonly MessageReceiver Receiver;

            public ReceiverContext(int topicIndex,
                                   MessageReceiver receiver,
                                   Action<int, IEnumerable<BrokeredMessage>> handler,
                                   Action<int, Exception> errorHandler)
            {
                _topicIndex = topicIndex;
                Receiver = receiver;
                _handler = handler;
                _errorHandler = errorHandler;
                ReceiveTimeout = DefaultReadTimeout;
                ReceiveBatchSize = DefaultReceiveBatchSize;
            }

            public TimeSpan ReceiveTimeout { get; set; }
            public int ReceiveBatchSize { get; set; }

            public void OnError(Exception ex)
            {
                _errorHandler(_topicIndex, ex);
            }

            public void OnMessage(IEnumerable<BrokeredMessage> messages)
            {
                _handler(_topicIndex, messages);
            }
        }
    }
}
