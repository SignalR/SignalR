using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class TestPerformanceCounterManager : IPerformanceCounterManager
    {
        public IPerformanceCounter ConnectionsConnected => new TestPerformanceCounter(nameof(ConnectionsConnected));

        public IPerformanceCounter ConnectionsReconnected => new TestPerformanceCounter(nameof(ConnectionsReconnected));

        public IPerformanceCounter ConnectionsDisconnected => new TestPerformanceCounter(nameof(ConnectionsDisconnected));

        public IPerformanceCounter ConnectionsCurrentForeverFrame => new TestPerformanceCounter(nameof(ConnectionsCurrentForeverFrame));

        public IPerformanceCounter ConnectionsCurrentLongPolling => new TestPerformanceCounter(nameof(ConnectionsCurrentLongPolling));

        public IPerformanceCounter ConnectionsCurrentServerSentEvents => new TestPerformanceCounter(nameof(ConnectionsCurrentServerSentEvents));

        public IPerformanceCounter ConnectionsCurrentWebSockets => new TestPerformanceCounter(nameof(ConnectionsCurrentWebSockets));

        public IPerformanceCounter ConnectionsCurrent => new TestPerformanceCounter(nameof(ConnectionsCurrent));

        public IPerformanceCounter ConnectionMessagesReceivedTotal => new TestPerformanceCounter(nameof(ConnectionMessagesReceivedTotal));

        public IPerformanceCounter ConnectionMessagesSentTotal => new TestPerformanceCounter(nameof(ConnectionMessagesSentTotal));

        public IPerformanceCounter ConnectionMessagesReceivedPerSec => new TestPerformanceCounter(nameof(ConnectionMessagesReceivedPerSec));

        public IPerformanceCounter ConnectionMessagesSentPerSec => new TestPerformanceCounter(nameof(ConnectionMessagesSentPerSec));

        public IPerformanceCounter MessageBusMessagesReceivedTotal => new TestPerformanceCounter(nameof(MessageBusMessagesReceivedTotal));

        public IPerformanceCounter MessageBusMessagesReceivedPerSec => new TestPerformanceCounter(nameof(MessageBusMessagesReceivedPerSec));

        public IPerformanceCounter ScaleoutMessageBusMessagesReceivedPerSec => new TestPerformanceCounter(nameof(ScaleoutMessageBusMessagesReceivedPerSec));

        public IPerformanceCounter MessageBusMessagesPublishedTotal => new TestPerformanceCounter(nameof(MessageBusMessagesPublishedTotal));

        public IPerformanceCounter MessageBusMessagesPublishedPerSec => new TestPerformanceCounter(nameof(MessageBusMessagesPublishedPerSec));

        public IPerformanceCounter MessageBusSubscribersCurrent => new TestPerformanceCounter(nameof(MessageBusSubscribersCurrent));

        public IPerformanceCounter MessageBusSubscribersTotal => new TestPerformanceCounter(nameof(MessageBusSubscribersTotal));

        public IPerformanceCounter MessageBusSubscribersPerSec => new TestPerformanceCounter(nameof(MessageBusSubscribersPerSec));

        public IPerformanceCounter MessageBusAllocatedWorkers => new TestPerformanceCounter(nameof(MessageBusAllocatedWorkers));

        public IPerformanceCounter MessageBusBusyWorkers => new TestPerformanceCounter(nameof(MessageBusBusyWorkers));

        public IPerformanceCounter MessageBusTopicsCurrent => new TestPerformanceCounter(nameof(MessageBusTopicsCurrent));

        public IPerformanceCounter ErrorsAllTotal => new TestPerformanceCounter(nameof(ErrorsAllTotal));

        public IPerformanceCounter ErrorsAllPerSec => new TestPerformanceCounter(nameof(ErrorsAllPerSec));

        public IPerformanceCounter ErrorsHubResolutionTotal => new TestPerformanceCounter(nameof(ErrorsHubResolutionTotal));

        public IPerformanceCounter ErrorsHubResolutionPerSec => new TestPerformanceCounter(nameof(ErrorsHubResolutionPerSec));

        public IPerformanceCounter ErrorsHubInvocationTotal => new TestPerformanceCounter(nameof(ErrorsHubInvocationTotal));

        public IPerformanceCounter ErrorsHubInvocationPerSec => new TestPerformanceCounter(nameof(ErrorsHubInvocationPerSec));

        public IPerformanceCounter ErrorsTransportTotal => new TestPerformanceCounter(nameof(ErrorsTransportTotal));

        public IPerformanceCounter ErrorsTransportPerSec => new TestPerformanceCounter(nameof(ErrorsTransportPerSec));

        public IPerformanceCounter ScaleoutStreamCountTotal => new TestPerformanceCounter(nameof(ScaleoutStreamCountTotal));

        public IPerformanceCounter ScaleoutStreamCountOpen => new TestPerformanceCounter(nameof(ScaleoutStreamCountOpen));

        public IPerformanceCounter ScaleoutStreamCountBuffering => new TestPerformanceCounter(nameof(ScaleoutStreamCountBuffering));

        public IPerformanceCounter ScaleoutErrorsTotal => new TestPerformanceCounter(nameof(ScaleoutErrorsTotal));

        public IPerformanceCounter ScaleoutErrorsPerSec => new TestPerformanceCounter(nameof(ScaleoutErrorsPerSec));

        public IPerformanceCounter ScaleoutSendQueueLength => new TestPerformanceCounter(nameof(ScaleoutSendQueueLength));

        public void Initialize(string instanceName, CancellationToken hostShutdownToken)
        {
        }

        public IPerformanceCounter LoadCounter(string categoryName, string counterName, string instanceName, bool isReadOnly) => new TestPerformanceCounter(counterName);

        private class TestPerformanceCounter : IPerformanceCounter
        {
            public string CounterName { get; }

            public long RawValue { get; set; }

            public TestPerformanceCounter(string counterName)
            {
                CounterName = counterName;
            }

            public void Close()
            {
            }

            public long Decrement()
            {
                RawValue--;
                return RawValue;
            }

            public long Increment()
            {
                RawValue++;
                return RawValue;
            }

            public long IncrementBy(long value)
            {
                RawValue += value;
                return RawValue;
            }

            public CounterSample NextSample()
            {
                return default(CounterSample);
            }

            public void RemoveInstance()
            {
            }
        }
    }
}
