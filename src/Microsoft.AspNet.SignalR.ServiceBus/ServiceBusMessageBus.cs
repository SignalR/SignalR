// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly ServiceBusConnectionContext _connectionContext;
        private readonly ServiceBusConnection _connection;
        private readonly string[] _topics;
        
        public ServiceBusMessageBus(IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Retrieve the trace manager
            var traceManager = resolver.Resolve<ITraceManager>();
            TraceSource trace = traceManager["SignalR." + typeof(ServiceBusMessageBus).Name];

            _connection = new ServiceBusConnection(configuration, trace);

            _topics = Enumerable.Range(0, configuration.TopicCount)
                                .Select(topicIndex => SignalRTopicPrefix + "_" + configuration.TopicPrefix + "_" + topicIndex)
                                .ToArray();

            _connectionContext = _connection.Subscribe(_topics, OnMessage, OnError);

            // Open the streams after creating the subscription
            for (int i = 0; i < configuration.TopicCount; i++)
            {
                Open(i);
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

            return _connectionContext.Publish(streamIndex, stream);
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
                if (_connectionContext != null)
                {
                    _connectionContext.Dispose();
                }

                if (_connection != null)
                {
                    _connection.Dispose();
                }
            }
        }
    }
}