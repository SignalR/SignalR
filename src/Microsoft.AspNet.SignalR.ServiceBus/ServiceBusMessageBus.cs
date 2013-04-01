// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        private const string SignalRTopicPrefix = "SIGNALR_TOPIC";

        private IDisposable _subscription;
        private ServiceBusConnection _connection;
        private readonly string[] _topics;

        public ServiceBusMessageBus(IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connection = new ServiceBusConnection(configuration.ConnectionString);

            _topics = Enumerable.Range(0, configuration.TopicCount)
                                .Select(topicIndex => SignalRTopicPrefix + "_" + configuration.TopicPrefix + "_" + topicIndex)
                                .ToArray();

            _subscription = _connection.Subscribe(_topics, OnMessage, Open);
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
            string topic = _topics[streamIndex];

            var stream = ServiceBusMessage.ToStream(messages);

            return _connection.Publish(topic, stream);
        }

        private void OnMessage(int topicIndex, IEnumerable<BrokeredMessage> messages)
        {
            foreach (var message in messages)
            {
                using (message)
                {
                    IList<Message> internalMessages = ServiceBusMessage.FromStream(message.GetBody<Stream>());

                    OnReceived(topicIndex, (ulong)message.EnqueuedSequenceNumber, internalMessages);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
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

            base.Dispose(disposing);
        }
    }
}
