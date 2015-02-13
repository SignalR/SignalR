using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.EventHub
{
    public class EventHubConnection : IDisposable
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        private readonly TimeSpan _backoffTime;
        private readonly TimeSpan _idleSubscriptionTimeout;
        private readonly NamespaceManager _namespaceManager;
        private readonly EventHubScaleoutConfiguration _configuration;
        private readonly string _connectionString;
        private readonly TraceSource _trace;
        private EventHubClient _eventHubClient;
        private EventHubConsumerGroup _consumerGroup;
        private ReceiverContext[] _receiverContexts;

        public EventHubConnection(EventHubScaleoutConfiguration configuration, TraceSource traceSource)
        {
            _trace = traceSource;
            _connectionString = configuration.BuildConnectionString();

            try
            {
                _namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
            }
            catch (ConfigurationErrorsException)
            {
                _trace.TraceError("The configured Service Bus connection string contains an invalid property. Check the exception details for more information.");
                throw;
            }

            _backoffTime = configuration.BackoffTime;
            _idleSubscriptionTimeout = configuration.IdleSubscriptionTimeout;    
            _configuration = configuration;
            _receiverContexts = new ReceiverContext[configuration.PartitionCount];
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The disposable is returned to the caller")]
        public void Subscribe(EventHubConnectionContext connectionContext)
        {
            if (connectionContext == null)
            {
                throw new ArgumentNullException("connectionContext");
            }
            var eventHubName = connectionContext.EventHubName;
            _trace.TraceInformation("Subscribing to {0} partition(s) in the event hub {1}...", connectionContext.PartitionCount, eventHubName);
            connectionContext.NamespaceManager = _namespaceManager;

            var sw = new Stopwatch();
            sw.Start();

            // Make sure we have an EventHub
            if (!_namespaceManager.EventHubExists(eventHubName))
            {
                _trace.TraceInformation("Creating a new event hub {0} in the service bus...", eventHubName);
                _namespaceManager.CreateEventHubIfNotExists(eventHubName);
                _trace.TraceInformation("Creation of a new event hub {0} in the service bus completed successfully.", eventHubName);
            }
            sw.Stop();
            Debug.WriteLine("EventHubExists time: {0} ms", sw.ElapsedMilliseconds);

            _namespaceManager.CreateConsumerGroupIfNotExists(eventHubName, _configuration.ConsumerGroupName);
            _trace.TraceInformation("Creation of a new consumer group {0} for event hub {1} in the service bus completed successfully.", _configuration.ConsumerGroupName, eventHubName);

            // Create a client for event hub
            _eventHubClient = EventHubClient.CreateFromConnectionString(_connectionString, eventHubName);
            if (_configuration.RetryPolicy != null)
            {
                _eventHubClient.RetryPolicy = _configuration.RetryPolicy;
            }
            else
            {
                _eventHubClient.RetryPolicy = RetryExponential.Default;
            }

            _consumerGroup = _eventHubClient.GetConsumerGroup(_configuration.ConsumerGroupName);


            for (var topicIndex = 0; topicIndex < connectionContext.PartitionCount; ++topicIndex)
            {
                Retry(() => CreatePartitionedSender(connectionContext, topicIndex, eventHubName));
            }

            _trace.TraceInformation("Subscription to {0} partition(s) in the service bus event hub service completed successfully.", connectionContext.PartitionCount);
        }

        private void CreatePartitionedSender(EventHubConnectionContext connectionContext, int topicIndex, string eventHubName)
        {
            lock (connectionContext.TopicClientsLock)
            {
                if (connectionContext.IsDisposed)
                {
                    return;
                }

                var partitionIds = _eventHubClient.GetRuntimeInformation().PartitionIds;
                var eventHubSender = _eventHubClient.CreatePartitionedSender(partitionIds[topicIndex]);
                connectionContext.SetEventHubSenders(eventHubSender, topicIndex);
                _trace.TraceInformation("Creation of a new event hub client {0} completed successfully.", eventHubName);
            }

            CreateReceiver(connectionContext, topicIndex, eventHubName);
        }

        private void CreateReceiver(EventHubConnectionContext connectionContext, int topicIndex, string eventHubName)
        {
            lock (connectionContext.SubscriptionsLock)
            {
                if (connectionContext.IsDisposed)
                {
                    return;
                }

                var partitionIds = _eventHubClient.GetRuntimeInformation().PartitionIds;
                var receiver = _consumerGroup.CreateReceiver(partitionIds[topicIndex], DateTime.UtcNow);
                _trace.TraceInformation("Creation of a new event hub receiver id {0} completed successfully.", topicIndex);
                var receiverContext = new ReceiverContext(topicIndex, receiver, connectionContext);
                _receiverContexts[topicIndex] = receiverContext;
                Task.Run(async () =>  await ProcessMessages(receiverContext));

                // Open the stream
                connectionContext.OpenStream(topicIndex);
            }
        }


        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We retry to create the topics on exceptions")]
        private void Retry(Action action)
        {
            string errorMessage = "Failed to create service bus event hub sender or recevier: {0}";
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
                    break;
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
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _trace.TraceError(errorMessage, ex.Message);
                    Thread.Sleep(RetryDelay);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_receiverContexts != null)
                {
                    for (int i = 0; i < _receiverContexts.Length; i++)
			        {
                        if (_receiverContexts[i].Receiver != null)
                        {
                            _receiverContexts[i].Receiver.Close();
                        }
		        	}
                }

                if (_eventHubClient != null)
                {
                    _eventHubClient.Close();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are handled through the error handler callback")]
        private async Task ProcessMessages(ReceiverContext receiverContext)
        {
            while (true)
            {
                bool delay = false;
                try
                {
                    Debug.WriteLine(string.Format("WAITING IN: {0}", receiverContext.PartitionIndex));
                    var messages = await receiverContext.Receiver.ReceiveAsync(32);
                    if (messages != null)
                    {
                        Debug.WriteLine(string.Format("MESSAGE IN: {0}", receiverContext.PartitionIndex));
                        receiverContext.OnMessage(messages);
                    }
                    else
                    {
                        Debug.WriteLine("Received null message from Event hub!");
                    }
                }
                catch (OperationCanceledException)
                {
                    // This means the channel is closed
                    _trace.TraceError("OperationCanceledException was thrown in trying to receive the message from the service bus.");

                    return;
                }
                catch (Exception ex)
                {
                    _trace.TraceError(ex.Message);
                    receiverContext.OnError(ex);
                    delay = true;
                }
                if (delay)
                    await Task.Delay(RetryDelay);
            }
        }

    }
}
