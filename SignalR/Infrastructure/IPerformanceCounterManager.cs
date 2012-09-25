using System.Diagnostics;
using System.Threading;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Initializes and writes to performance counters.
    /// </summary>
    public interface IPerformanceCounterManager
    {
        /// <summary>
        /// Initializes the performance counter instances.
        /// </summary>
        /// <param name="instanceName">The host instance name.</param>
        /// <param name="hostShutdownToken">The CancellationToken representing the host shutdown.</param>
        void Initialize(string instanceName, CancellationToken hostShutdownToken);

        // Connections count
        IPerformanceCounter ConnectionsConnected { get; }
        IPerformanceCounter ConnectionsReconnected { get; }
        IPerformanceCounter ConnectionsDisconnected { get; }
        IPerformanceCounter ConnectionsCurrent { get; }

        // Connections throughput
        IPerformanceCounter ConnectionMessagesReceivedTotal { get; }
        IPerformanceCounter ConnectionMessagesSentTotal { get; }
        IPerformanceCounter ConnectionMessagesReceivedPerSec { get; }
        IPerformanceCounter ConnectionMessagesSentPerSec { get; }

        // Message bus
        IPerformanceCounter MessageBusMessagesPublishedTotal { get; }
        IPerformanceCounter MessageBusMessagesPublishedPerSec { get; }
        IPerformanceCounter MessageBusSubscribersCurrent { get; }
        IPerformanceCounter MessageBusSubscribersTotal { get; }
        IPerformanceCounter MessageBusSubscribersPerSec { get; }
        IPerformanceCounter MessageBusAllocatedWorkers { get; }
        IPerformanceCounter MessageBusBusyWorkers { get; }

        // Errors
        IPerformanceCounter ErrorsAllTotal { get; }
        IPerformanceCounter ErrorsAllPerSec { get; }
        IPerformanceCounter ErrorsHubResolutionTotal { get; }
        IPerformanceCounter ErrorsHubResolutionPerSec { get; }
        IPerformanceCounter ErrorsHubInvocationTotal { get; }
        IPerformanceCounter ErrorsHubInvocationPerSec { get; }
        IPerformanceCounter ErrorsTransportTotal { get; }
        IPerformanceCounter ErrorsTransportPerSec { get; }
    }
}
