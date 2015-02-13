using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.EventHub
{
    public class EventHubConnectionContext : IDisposable
    {
        private readonly EventHubScaleoutConfiguration _configuration;
        private readonly EventHubSender[] _eventHubSenders;
        private readonly TraceSource _trace;

        public object SubscriptionsLock { get; private set; }
        public object TopicClientsLock { get; private set; }

        public int PartitionCount { get; private set; }
        public string EventHubName { get; private set; }
        public Action<int, IEnumerable<EventData>> EventDataHandler { get; private set; }
        public Action<int, Exception> ErrorHandler { get; private set; }
        public Action<int> OpenStream { get; private set; }

        public bool IsDisposed { get; private set; }

        public NamespaceManager NamespaceManager { get; set; }

        public EventHubConnectionContext(EventHubScaleoutConfiguration configuration,
                                           string eventHubName,
                                           TraceSource traceSource,
                                           Action<int, IEnumerable<EventData>> eventDataHandler,
                                           Action<int, Exception> errorHandler,
                                           Action<int> openStream)
        {
            if (eventHubName == null)
            {
                throw new ArgumentNullException("eventHubName");
            }

            _configuration = configuration;

            _eventHubSenders = new EventHubSender[configuration.PartitionCount];
            PartitionCount = configuration.PartitionCount;

            _trace = traceSource;

            EventHubName = eventHubName;
            EventDataHandler = eventDataHandler;
            ErrorHandler = errorHandler;
            OpenStream = openStream;

            TopicClientsLock = new object();
            SubscriptionsLock = new object();
        }

        public Task EventHubPublish(int topicIndex, Stream stream)
        {
            if (IsDisposed)
            {
                return TaskAsyncHelper.Empty;
            }

            var message = new EventData(stream);

            Debug.WriteLine(string.Format("MESSAGE OUT: {0}", topicIndex));


            //message.PartitionKey = topicIndex.ToString();
            return _eventHubSenders[topicIndex].SendAsync(message);
        }

        internal void SetEventHubSenders(EventHubSender eventHubSender, int eventHubIndex)
        {
            lock (TopicClientsLock)
            {
                if (!IsDisposed)
                {
                    _eventHubSenders[eventHubIndex] = eventHubSender;
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
                            if (_eventHubSenders != null)
                            {
                                for (int i = 0; i < _eventHubSenders.Length; i++)
                                {
                                    // BUG #2937: We need to null check here because the given topic/subscription
                                    // may never have actually been created due to the lock being released
                                    // between each retry attempt

                                    var eventHubSender = _eventHubSenders[i];
                                    if (eventHubSender != null)
                                    {
                                        eventHubSender.Close();
                                    }

                                    //var subscription = _subscriptions[i];
                                    //if (subscription != null)
                                    //{
                                    //    subscription.Receiver.Close();
                                    //    NamespaceManager.DeleteSubscription(subscription.TopicPath, subscription.Name);
                                    //}
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
