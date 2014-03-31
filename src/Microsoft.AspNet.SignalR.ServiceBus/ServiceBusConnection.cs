// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    internal class ServiceBusConnection : IDisposable
    {
        private const int DefaultReceiveBatchSize = 1000;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        private readonly TimeSpan _idleSubscriptionTimeout;
        private readonly NamespaceManager _namespaceManager;
        private readonly MessagingFactory _factory;
        private readonly ServiceBusScaleoutConfiguration _configuration;
        private readonly string _connectionString;
        private readonly TraceSource _trace;

        public ServiceBusConnection(ServiceBusScaleoutConfiguration configuration, TraceSource traceSource)
        {
            _trace = traceSource;
            _connectionString = configuration.BuildConnectionString();

            try
            {
                _namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
                _factory = MessagingFactory.CreateFromConnectionString(_connectionString);

                if (configuration.RetryPolicy != null)
                {
                    _factory.RetryPolicy = configuration.RetryPolicy;
                }
                else
                {
                    _factory.RetryPolicy = RetryPolicy.Default;
                }
            }
            catch (ConfigurationErrorsException)
            {
                _trace.TraceError("The configured Service Bus connection string contains an invalid property. Check the exception details for more information.");
                throw;
            }

            _idleSubscriptionTimeout = configuration.IdleSubscriptionTimeout;
            _configuration = configuration;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable is returned to the caller")]
        public void Subscribe(ServiceBusConnectionContext connectionContext)
        {
            if (connectionContext == null)
            {
                throw new ArgumentNullException("connectionContext");
            }

            _trace.TraceInformation("Subscribing to {0} topic(s) in the service bus...", connectionContext.TopicNames.Count);

            connectionContext.NamespaceManager = _namespaceManager;

            for (var topicIndex = 0; topicIndex < connectionContext.TopicNames.Count; ++topicIndex)
            {
                Exception exception;
                Action callback = () => CreateTopic(connectionContext, topicIndex);

                if (!Retry(callback, out exception))
                {
                    // We failed to initialize this stream to report the error
                    connectionContext.ErrorHandler(topicIndex, exception);
                }
            }

            _trace.TraceInformation("Subscription to {0} topics in the service bus Topic service completed successfully.", connectionContext.TopicNames.Count);
        }

        private void CreateTopic(ServiceBusConnectionContext connectionContext, int topicIndex)
        {
            lock (connectionContext.TopicClientsLock)
            {
                if (connectionContext.IsDisposed)
                {
                    return;
                }

                string topicName = connectionContext.TopicNames[topicIndex];

                if (!_namespaceManager.TopicExists(topicName))
                {
                    try
                    {
                        _trace.TraceInformation("Creating a new topic {0} in the service bus...", topicName);

                        _namespaceManager.CreateTopic(topicName);

                        _trace.TraceInformation("Creation of a new topic {0} in the service bus completed successfully.", topicName);

                    }
                    catch (MessagingEntityAlreadyExistsException)
                    {
                        // The entity already exists
                        _trace.TraceInformation("Creation of a new topic {0} threw an MessagingEntityAlreadyExistsException.", topicName);
                    }
                }

                // Create a client for this topic
                TopicClient topicClient = TopicClient.CreateFromConnectionString(_connectionString, topicName);

                if (_configuration.RetryPolicy != null)
                {
                    topicClient.RetryPolicy = _configuration.RetryPolicy;
                }
                else
                {
                    topicClient.RetryPolicy = RetryExponential.Default;
                }

                connectionContext.SetTopicClients(topicClient, topicIndex);

                _trace.TraceInformation("Creation of a new topic client {0} completed successfully.", topicName);
            }

            CreateSubscription(connectionContext, topicIndex);
        }

        private void CreateSubscription(ServiceBusConnectionContext connectionContext, int topicIndex)
        {
            lock (connectionContext.SubscriptionsLock)
            {
                if (connectionContext.IsDisposed)
                {
                    return;
                }

                string topicName = connectionContext.TopicNames[topicIndex];

                // Create a random subscription
                string subscriptionName = Guid.NewGuid().ToString();

                try
                {
                    var subscriptionDescription = new SubscriptionDescription(topicName, subscriptionName);

                    // This cleans up the subscription while if it's been idle for more than the timeout.
                    subscriptionDescription.AutoDeleteOnIdle = _idleSubscriptionTimeout;

                    _namespaceManager.CreateSubscription(subscriptionDescription);

                    _trace.TraceInformation("Creation of a new subscription {0} for topic {1} in the service bus completed successfully.", subscriptionName, topicName);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // The entity already exists
                    _trace.TraceInformation("Creation of a new subscription {0} for topic {1} threw an MessagingEntityAlreadyExistsException.", subscriptionName, topicName);
                }

                // Create a receiver to get messages
                string subscriptionEntityPath = SubscriptionClient.FormatSubscriptionPath(topicName, subscriptionName);
                MessageReceiver receiver = _factory.CreateMessageReceiver(subscriptionEntityPath, ReceiveMode.ReceiveAndDelete);
                receiver.RetryPolicy = _configuration.RetryPolicy ?? RetryPolicy.Default;

                _trace.TraceInformation("Creation of a message receive for subscription entity path {0} in the service bus completed successfully.", subscriptionEntityPath);

                connectionContext.SetSubscriptionContext(new SubscriptionContext(topicName, subscriptionName, receiver), topicIndex);

                var receiverContext = new ReceiverContext(topicIndex, receiver, connectionContext);

                ProcessMessages(receiverContext);

                // Open the stream
                connectionContext.OpenStream(topicIndex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We retry to create the topics on exceptions")]
        private bool Retry(Action action, out Exception exception)
        {
            string errorMessage = "Failed to create service bus subscription or topic : {0}";
            exception = null;

            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _trace.TraceError(errorMessage, ex.Message);
                    exception = ex;
                    return false;
                }
                catch (MessagingException ex)
                {
                    _trace.TraceError(errorMessage, ex.Message);

                    if (ex.IsTransient)
                    {
                        Thread.Sleep(RetryDelay);
                    }
                    else
                    {
                        exception = ex;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _trace.TraceError(errorMessage, ex.Message);
                    exception = ex;
                    return false;
                }
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Close the factory
                if (_factory != null)
                {
                    _factory.Close();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are handled through the error handler callback")]
        private void ProcessMessages(ReceiverContext receiverContext)
        {
            var options = new OnMessageOptions();
            options.ExceptionReceived += (sender, e) =>
            {
                receiverContext.OnError(e.Exception);
            };

            receiverContext.Receiver.OnMessage(message =>
            {
                receiverContext.OnMessage(message);
            },
            options);
        }

        private class ReceiverContext
        {
            public readonly MessageReceiver Receiver;
            public readonly ServiceBusConnectionContext ConnectionContext;

            private readonly int _topicIndex;

            public ReceiverContext(int topicIndex,
                                   MessageReceiver receiver,
                                   ServiceBusConnectionContext connectionContext)
            {
                _topicIndex = topicIndex;
                Receiver = receiver;
                ConnectionContext = connectionContext;
            }

            public void OnError(Exception ex)
            {
                ConnectionContext.ErrorHandler(_topicIndex, ex);
            }

            public void OnMessage(BrokeredMessage message)
            {
                ConnectionContext.Handler(_topicIndex, message);
            }
        }
    }
}
