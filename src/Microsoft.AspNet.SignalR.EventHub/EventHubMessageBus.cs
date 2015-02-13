using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.EventHub
{
    /// <summary>
    /// Uses Windows Azure event hubs to scale-out SignalR applications in web farms.
    /// </summary>
    public class EventHubMessageBus : ScaleoutMessageBus
    {
        private EventHubConnectionContext _connectionContext;
        private TraceSource _trace;
        private readonly EventHubConnection _connection;

        public EventHubMessageBus(IDependencyResolver resolver, EventHubScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Retrieve the trace manager
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(EventHubMessageBus).Name];

            _connection = new EventHubConnection(configuration, _trace);
            _connectionContext = new EventHubConnectionContext(configuration, configuration.EventHubName, _trace, EventDataOnMessage, OnError, Open);

            ThreadPool.QueueUserWorkItem(Subscribe);
        }

        protected override int StreamCount
        {
            get
            {
                return _connectionContext.PartitionCount;
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var stream = EventHubMessage.ToStream(messages);

            Debug.WriteLine(string.Format("Send: {0}", messages.Count));
            foreach (var msg in messages)
            {
                string v = null;
                if (msg.Value != null)
                {
                    v = Encoding.UTF8.GetString(msg.Value.ToArray());
                }
                Debug.WriteLine(string.Format("Send: CommandId: {0}, Encoding: {1}, Filter: {2}, IsAck: {3}, IsCommand: {4}, Key: {5}, MappingId: {6}, Source: {7}, StreamIndex: {8}, Value:'{9}', WaitForAck: {10}", 
                    msg.CommandId, msg.Encoding, msg.Filter, msg.IsAck, msg.IsCommand, msg.Key, msg.MappingId, msg.Source, msg.StreamIndex, v, msg.WaitForAck));
            }

            TraceMessages(messages, "Sending");

            return _connectionContext.EventHubPublish(streamIndex, stream);
        }

        private void EventDataOnMessage(int topicIndex, IEnumerable<EventData> messages)
        {
            Debug.WriteLine("Message from event hub!!!");
            if (!messages.Any())
            {
                // Force the topic to re-open if it was ever closed even if we didn't get any messages
                Open(topicIndex);
            }

            foreach (var message in messages)
            {
                using (message)
                {
                    ScaleoutMessage scaleoutMessage = EventHubMessage.FromEventData(message);

                    TraceMessages(scaleoutMessage.Messages, "Receiving");
                    Debug.WriteLine("Got {0} messages from event hub. message.SequenceNumber: {1}", scaleoutMessage.Messages.Count, message.SequenceNumber);
                    foreach (var item in scaleoutMessage.Messages)
                    {
                        string v = string.Empty;
                        if (item.Value != null)
                        {
                            v = Encoding.UTF8.GetString(item.Value.ToArray());    
                        }
                        Debug.WriteLine("{0}, {1}, {2}, {3}", item.CommandId, item.Filter, item.Key, v);
                        
                    }
                    OnReceived(topicIndex, (ulong)message.SequenceNumber, scaleoutMessage);
                }
            }
        }

        private void Subscribe(object state)
        {
            _connection.Subscribe(_connectionContext);
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
