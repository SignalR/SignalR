
using System.Diagnostics;
using System.Threading;
namespace SignalR.Infrastructure
{
    /// <summary>
    /// Initializes and writes to performance counters.
    /// </summary>
    public interface IPerformanceCounterWriter
    {
        /// <summary>
        /// Initializes the performance counter instances for the specified HostContext.
        /// </summary>
        /// <param name="instanceName">The host instance name.</param>
        /// <param name="hostShutdownToken">The CancellationToken representing the host shutdown.</param>
        void Initialize(string instanceName, CancellationToken hostShutdownToken);

        /// <summary>
        /// Gets the performance counter with the specified name.
        /// </summary>
        /// <param name="counterName">The performance counter name.</param>
        /// <returns>The performance counter with the given name, or null if not found.</returns>
        PerformanceCounter GetCounter(string counterName);
    }
}
