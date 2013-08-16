// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    /// <summary>
    /// Uses Windows Azure Service Bus topics to scale-out SignalR applications in web farms.
    /// </summary>
    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        private const string SignalRTopicPrefix = "SIGNALR_TOPIC";

        private ServiceBusSubscription _subscription;
        private readonly ServiceBusConnection _connection;
        private readonly string[] _topics;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);
        private readonly TraceSource _trace;

        public ServiceBusMessageBus(IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Retrieve the trace manager
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ServiceBusMessageBus).Name];

            _connection = new ServiceBusConnection(configuration, _trace);

            _topics = Enumerable.Range(0, configuration.TopicCount)
                                .Select(topicIndex => SignalRTopicPrefix + "_" + configuration.TopicPrefix + "_" + topicIndex)
                                .ToArray();

            SubscribeWithRetry(configuration.TopicCount);
        }

        private void SubscribeWithRetry(int topicCount)
        {
            Task subscribeTask = SubscribeToServiceBus();

            subscribeTask.ContinueWith(task =>
            {
                if (task.IsFaulted && !task.Exception.InnerExceptions.Any(i => i is MessagingEntityAlreadyExistsException))
                {
                    if (task.Exception.InnerExceptions.Any(i => i is UnauthorizedAccessException))
                        return;
                    if (task.Exception.InnerExceptions.Any(i => i is QuotaExceededException))
                        return;
                    TaskAsyncHelper.Delay(_retryDelay)
                                   .Then(bus => bus.SubscribeWithRetry(topicCount), this);
                }
                else
                {
                    _connection.ProcessReceivers(OnMessage, OnError);

                    // Open the streams after creating the subscription
                    for (int i = 0; i < topicCount; i++)
                    {
                        Open(i);
                    }
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        private Task SubscribeToServiceBus()
        {
            try
            {
                _subscription = _connection.Subscribe(_topics, OnMessage, OnError);

                return TaskAsyncHelper.Empty;
            }
            catch (Exception ex)
            {
                _trace.TraceError("Error Subscribe to ServiceBus - " + ex.GetBaseException());

                return TaskAsyncHelper.FromError(ex);
            }
        }


        protected override int StreamCount
        {
            get
            {
                return _topics.Length;
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var stream = ServiceBusMessage.ToStream(messages);

            return _subscription.Publish(streamIndex, stream).Catch(exception =>
            {
                // re-up subscription
                if (exception.InnerExceptions.Any(i => i is MessagingEntityNotFoundException))
                    SubscribeWithRetry(_topics.Length);
            });
        }

        private void OnMessage(int topicIndex, IEnumerable<BrokeredMessage> messages)
        {
            if (!messages.Any())
            {
                // Force the topic to re-open if it was ever closed even if we didn't get any messages
                Open(topicIndex);
            }

            foreach (var message in messages)
            {
                using (message)
                {
                    ScaleoutMessage scaleoutMessage = ServiceBusMessage.FromBrokeredMessage(message);

                    OnReceived(topicIndex, (ulong)message.EnqueuedSequenceNumber, scaleoutMessage);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_subscription != null)
                {
                    _subscription.Dispose();
                }

                if (_connection != null)
                {
                    _connection.Dispose();
                }
            }
        }
    }
}
