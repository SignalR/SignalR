// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    /// <summary>
    /// Uses Windows Azure Service Bus topics to scale-out SignalR applications in web farms.
    /// </summary>
    public class ServiceBusMessageBus : ScaleoutMessageBus
    {
        private readonly ServiceBusScaleoutConfiguration _configuration;
        private const string SignalRTopicPrefix = "SIGNALR_TOPIC";

        private SubscriptionClient _client;
        private TopicClient _sender;
        private readonly string _runId = Guid.NewGuid().ToString();
        private readonly TraceSource _trace;
        private readonly ServiceBusFactory _serviceBusFactory;

        public ServiceBusMessageBus(IDependencyResolver resolver, ServiceBusScaleoutConfiguration configuration) : base(resolver, configuration)
        {
            _configuration = configuration;
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _serviceBusFactory = new ServiceBusFactory(configuration.BuildConnectionString());

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ServiceBusMessageBus).Name];

            SetupBus();
        }

        private void SetupBus()
        {
            var options = new OnMessageOptions
            {
                AutoComplete = true,
                MaxConcurrentCalls = 1,
            };
            options.ExceptionReceived += ServicebusError;

            var topicName = SignalRTopicPrefix + "_" + _configuration.TopicPrefix + "_0";
            var topic = _serviceBusFactory.CreateIfNotExists(new TopicDescription(topicName)
            {
                MaxSizeInMegabytes = 1024,
                DefaultMessageTimeToLive = _configuration.TimeToLive,
                EnableExpress = true,
                RequiresDuplicateDetection = false,
            });

            var subscription = _serviceBusFactory.CreateIfNotExists(new SubscriptionDescription(topic.Path, _runId)
            {
                AutoDeleteOnIdle = _configuration.IdleSubscriptionTimeout,
                DefaultMessageTimeToLive = _configuration.TimeToLive,
                EnableDeadLetteringOnMessageExpiration = false,
            });

            _client = _serviceBusFactory.GetTopicClient(subscription, ReceiveMode.ReceiveAndDelete);
            _client.PrefetchCount = 32;
            _client.RetryPolicy = new RetryExponential(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60), 100000);
            _client.OnMessage(HandleBusMessage, options);
            _sender = _serviceBusFactory.GetTopic(topic);

            Open(0);
        }

        protected override async Task Send(int streamIndex, IList<Message> messages)
        {
            TraceMessages(messages, "Sending");

            var stream = ServiceBusMessage.ToStream(messages);
            var brokeredMessage = new BrokeredMessage(stream)
            {
                TimeToLive = _configuration.TimeToLive,
            };

            await _sender.SendAsync(brokeredMessage);
        }

        private void HandleBusMessage(BrokeredMessage message)
        {
            try
            {
                var scaleoutMessage = ServiceBusMessage.FromBrokeredMessage(message);
                TraceMessages(scaleoutMessage.Messages, "Receiving");
                OnReceived(0, (ulong)message.SequenceNumber, scaleoutMessage);
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex.Message);
            }
        }

        private void ServicebusError(object sender, ExceptionReceivedEventArgs e)
        {
            _trace.TraceError(e.Exception.Message);
        }

        private void TraceMessages(IList<Message> messages, string messageType)
        {
            if (!_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                return;
            }

            foreach (Message message in messages)
            {
                _trace.TraceVerbose("{0} {1} bytes over Service Bus: {2}", messageType, message.Value.Array.Length, message.GetString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sender?.Close();
                _client?.Close();

                _sender = null;
                _client = null;
            }

            base.Dispose(disposing);
        }
    }
}
