
using System.Diagnostics;
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
        /// <param name="hostContext">The HostContext.</param>
        void Initialize(HostContext hostContext);

        /// <summary>
        /// Gets the performance counter with the specified name.
        /// </summary>
        /// <param name="counterName">The performance counter name.</param>
        /// <returns>The performance counter with the given name, or null if not found.</returns>
        PerformanceCounter GetCounter(string counterName);
    }
}
