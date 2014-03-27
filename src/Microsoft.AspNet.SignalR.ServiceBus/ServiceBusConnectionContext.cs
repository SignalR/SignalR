// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusConnectionContext : IDisposable
    {
        private readonly ServiceBusScaleoutConfiguration _configuration;

        private readonly SubscriptionContext[] _subscriptions;
        private readonly TopicClient[] _topicClients;

        private readonly TraceSource _trace;

        public object SubscriptionsLock { get; private set; }
        public object TopicClientsLock { get; private set; }

        public IList<string> TopicNames { get; private set; }
        public Action<int, IEnumerable<BrokeredMessage>> Handler { get; private set; }
        public Action<int, Exception> ErrorHandler { get; private set; }
        public Action<int> OpenStream { get; private set; }

        public bool IsDisposed { get; private set; }

        public NamespaceManager NamespaceManager { get; set; }

        public ServiceBusConnectionContext(ServiceBusScaleoutConfiguration configuration,
                                           IList<string> topicNames,
                                           TraceSource traceSource,
                                           Action<int, IEnumerable<BrokeredMessage>> handler,
                                           Action<int, Exception> errorHandler,
                                           Action<int> openStream)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException("topicNames");
            }

            _configuration = configuration;

            _subscriptions = new SubscriptionContext[topicNames.Count];
            _topicClients = new TopicClient[topicNames.Count];

            _trace = traceSource;

            TopicNames = topicNames;
            Handler = handler;
            ErrorHandler = errorHandler;
            OpenStream = openStream;

            TopicClientsLock = new object();
            SubscriptionsLock = new object();
        }

        public Task Publish(int topicIndex, Stream stream)
        {
            if (IsDisposed)
            {
                return TaskAsyncHelper.Empty;
            }

            var message = new BrokeredMessage(stream, ownsStream: true)
            {
                TimeToLive = _configuration.TimeToLive
            };

            if (message.Size > _configuration.MaximumMessageSize)
            {
                _trace.TraceWarning("Message size {0}KB exceeds the maximum size limit of {1}KB : {2}", message.Size / 1024, _configuration.MaximumMessageSize / 1024, message);
            }

            return _topicClients[topicIndex].SendAsync(message);
        }

        internal void SetSubscriptionContext(SubscriptionContext subscriptionContext, int topicIndex)
        {
            lock (SubscriptionsLock)
            {
                if (!IsDisposed)
                {
                    _subscriptions[topicIndex] = subscriptionContext;
                }
            }
        }

        internal void SetTopicClients(TopicClient topicClient, int topicIndex)
        {
            lock (TopicClientsLock)
            {
                if (!IsDisposed)
                {
                    _topicClients[topicIndex] = topicClient;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed)
                {
                    lock (TopicClientsLock)
                    {
                        lock (SubscriptionsLock)
                        {
                            for (int i = 0; i < TopicNames.Count; i++)
                            {
                                // BUG #2937: We need to null check here because the given topic/subscription
                                // may never have actually been created due to the lock being released
                                // between each retry attempt
                                var topicClient = _topicClients[i];
                                if (topicClient != null)
                                {
                                    topicClient.Close();
                                }

                                var subscription = _subscriptions[i];
                                if (subscription != null)
                                {
                                    subscription.Receiver.Close();
                                    NamespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);
                                }
                            }

                            IsDisposed = true;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
